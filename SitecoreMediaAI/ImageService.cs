using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Eventing;
using Sitecore.Pipelines.Upload;
using SitecoreMediaAI.Model;

namespace SitecoreMediaAI
{

    public class ImageService : UploadProcessor
    {
        public void Process(UploadArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            for (int index = 0; index < args.Files.Count; ++index)
            {
                HttpPostedFile file1 = args.Files[index];
                var alternateText = args.GetFileParameter(file1.FileName, "alt");
                if (string.IsNullOrEmpty(alternateText))
                {                
                    Stream imageStream = new MemoryStream();
                    file1.InputStream.CopyTo(imageStream);
                    file1.InputStream.Position = imageStream.Position = 0;
                   
                    string alt = AnalyseImage(imageStream).Result;
                    if (!args.Language.ToString().Contains("en")) {
                        alt = TranslateText(alt, args.Language.ToString()).Result;
                    }
                    args.SetFileParameter(file1.FileName, "alt", alt);
                }
                Log.Info("media upload alt=" + alternateText, this);
            }
        }

        static async Task<string> AnalyseImage(Stream imagebyteData)
        {
            Log.Info("start AnalyseImage", imagebyteData);
            var key = Sitecore.Configuration.Settings.GetSetting("SitecoreMediaAnalyseImage.Azure.Ocp-Apim-Subscription-Key");
            Assert.ArgumentNotNullOrEmpty(key, "The SitecoreMediaAnalyseImage.Azure.Ocp-Apim-Subscription-Key is Empty");
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            var responseText = string.Empty;

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            // Request parameters
            queryString["visualFeatures"] = "Description";
            var uri = "https://westus.api.cognitive.microsoft.com/vision/v1.0/analyze?" + queryString;

            HttpResponseMessage response;

            using (var content = new StreamContent(imagebyteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content).ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!String.IsNullOrEmpty(responseJson))
                {
                    var o = JsonConvert.DeserializeObject<VisonResponse>(responseJson);
                    responseText = o.description.captions.First().text;
                }
            }
            return await Task.Run(() => responseText);
        }


        //google is also an option translate https://translate.googleapis.com/translate_a/single?client=gtx&sl=nl-nl&tl=en&dt=t&q=jongen%20speelt%20piraat
        static async Task<string> TranslateText(string input, string languagePair)
        {
            var key = Sitecore.Configuration.Settings.GetSetting("SitecoreMediaTranslate.Azure.Ocp-Apim-Subscription-Key");
            Assert.ArgumentNotNullOrEmpty(key, "The SitecoreMediaTranslate.Azure.Ocp-Apim-Subscription-Key is Empty");
            var languageCode = languagePair.Substring(0, 2);
            Log.Info("Translate " + HttpUtility.UrlEncode(input) + " Language=" + languagePair, input);

            var client = new HttpClient();
            var tokenservice = new AzureAuthToken(key);
           
            client.DefaultRequestHeaders.Add("Authorization", tokenservice.GetAccessToken());

            var uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(input) + "&to="+ languageCode;

            string xmlresult = await client.GetStringAsync(uri).ConfigureAwait(false);


            string result = Deserialize<String>(xmlresult);


            client.Dispose();
            Log.Info("Translate done return", result);
            return await Task.Run(() => result);
        }

       public static T Deserialize<T>(string rawXml)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(rawXml)))
            {
                DataContractSerializer formatter0 =
                    new DataContractSerializer(typeof(T));
                return (T)formatter0.ReadObject(reader);
            }
        }
        
    }
}
