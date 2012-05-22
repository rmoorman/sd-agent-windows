using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using log4net;
using Newtonsoft.Json;
using System.Reflection;

namespace BoxedIce.ServerDensity.Agent
{
    /// <summary>
    /// Class to POST the agent payload data to the Server Density servers.
    /// </summary>
    public class PayloadPoster
    {
        /// <summary>
        /// Initialises a new instance of the PayloadPoster class with the 
        /// provided values.
        /// </summary>
        /// <param name="results">The payload dictionary.</param>
        public PayloadPoster(AgentConfigurationSection config, IDictionary<string, object> results)
        {
            _config = config;
            _results = results;
            _results.Add("os", "windows");
            _results.Add("agentKey", _config.AgentKey);

            try
            {
                _results.Add("internalHostname", Environment.MachineName);
            }
            catch (InvalidOperationException)
            {
            }

            if (_version == null)
            {
                Assembly asm = Assembly.Load(File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BoxedIce.ServerDensity.Agent.exe")));
                Version installedVersion = asm.GetName().Version;
                _version = installedVersion.ToString();
            }

            _results.Add("agentVersion", _version);
        }

        /// <summary>
        /// Creates and sends the HTTP POST.
        /// </summary>
        public void Post()
        {
            String payload = String.Empty;
            try
            {
                payload = JsonConvert.SerializeObject(_results);
            }
            catch
            {
                // if we get here, we've got a problem with the serialization
                var mongoDict = (_results["mongoDB"] as IDictionary<string, object>);

                // iterate the keys and attempt to convert each key
                foreach (var result in mongoDict.Keys)
                {
                    Log.InfoFormat("Attempting conversion of {0}", result);
                    try
                    {
                        // attempt the conversion
                        var conversion = JsonConvert.SerializeObject(mongoDict[result]);
                    }
                    catch
                    {
                        // if we get here, we've found the key that has the problem above
                        // it's probably a dictionary
                        // attempt a conversion of each key of that
                        IDictionary<string, object> inner = mongoDict[result] as IDictionary<string, object>;
                        foreach (var possible in inner.Keys)
                        {
                            // this will fail when it hits the value that can't be converted
                            
                            Log.InfoFormat("Processing possible failure key: {0}", possible);
                            Log.InfoFormat("Processing value: {0}", inner[possible].ToString());
                            var conversion = JsonConvert.SerializeObject(inner[possible]);
                            Log.InfoFormat("Processed: {0}", conversion);
                        }

                    }
                }
            }
            var hash = MD5Hash(payload);

            // TODO: this is for quick testing; we'll need to add proxy 
            //       settings, read the response, etc.
            var client = new WebClient();
            var data = new NameValueCollection();
            data.Add("payload", payload);
            Log.Debug(payload);
            data.Add("hash", hash);
            var url = string.Format("{0}{1}postback/", _config.ServerDensityUrl, _config.ServerDensityUrl.EndsWith("/") ? "" : "/");
            Log.InfoFormat("Posting to {0}", url);
            
            if (HttpWebRequest.DefaultWebProxy != null)
            {
                client.Proxy = HttpWebRequest.DefaultWebProxy;
            }

            byte[] response = client.UploadValues(url, "POST", data);
            string responseText = Encoding.ASCII.GetString(response);

            client.Dispose();
            
            if (responseText != "OK")
            {
                Log.ErrorFormat("URL {0} returned: {1}", url, responseText);
            }
            
            Log.Debug(responseText);
        }

        private static string MD5Hash(string input)
        {
            MD5CryptoServiceProvider x = new MD5CryptoServiceProvider();
            byte[] bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            StringBuilder s = new StringBuilder();

            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }

            return s.ToString();
        }

        private IDictionary<string, object> _results;
        private readonly AgentConfigurationSection _config;
        private static string _version;
        private readonly static ILog Log = LogManager.GetLogger(typeof(PayloadPoster));
    }
}
