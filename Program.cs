using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoImageCheck;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;

namespace SearchQueryKeywordFixes
{
    class Program
    {
        static int counter = 0;
        static int flagged = 0;
        static int newNameCounter = 0;
        static int patchedProducts = 0;
        static object flaggedLock = new object();
        static object counterLock = new object();
        static object patchedLock = new object();

        static Stopwatch timer;
        static JArray flaggedProducts;

        static void Main(string[] args)
        {
            timer = new Stopwatch();
            timer.Start();
            flaggedProducts = new JArray();

            string token = GetToken();
            ProcessAllProducts(token);

            using (StreamWriter sw = new StreamWriter(Settings.savePath))
            {
                sw.WriteLine("erpNumber" + "\t" + "name" + "\t" +"new name" + "\t"+ "new keyword");
                foreach (var item in flaggedProducts)
                {
                    if (item != null)
                    {
                        string newLine = item["erpNumber"] + "\t" + item["name"] + "\t" + item["newName"] +"\t" + item["newKeyword"] + "\t";
                        sw.WriteLine(newLine);
                    }
                }
                sw.Close();
            }
            timer.Stop();
            Console.WriteLine("Time to process product catalog from ecommsite: " + timer.Elapsed.Duration().ToString());
            Console.WriteLine("Total Products pulled from Ecomm ->" + counter.ToString());
            Console.WriteLine("Total flagged and updated keywords ->" + flagged.ToString());
            Console.WriteLine("Total products with updated name ->" + newNameCounter.ToString());
            Console.WriteLine("Total patched products ->" + patchedProducts.ToString());
        }

        static void ProcessAllProducts(string token)
        {
            JToken fullProductList = QueryAPIAllProducts(token);
            Parallel.ForEach(fullProductList, product =>
            {
                lock (counterLock)
                {
                    counter++;
                }
                string prodName = (string)product["shortDescription"];
                string newKeyword = NewKeyword(prodName);
                string newName = NewName(prodName);

                
                if (!newKeyword.Equals(prodName))
                {
                    string keywords = (string)product["metaKeywords"];
                    string[] keywordsArray = keywords.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(keyword => keyword.Trim()).ToArray();
                    if (!keywordsArray.Contains(newKeyword))
                    {
                    if (!newName.Equals(prodName))
                    {
                        AddProductToList(product, newName, newKeyword);

                            //test for specific product
                            if (((string)product["erpNumber"]).Equals("1479285"))
                            {
                                if (PatchProduct((string)product["id"], keywordsArray, newName, newKeyword, token))
                                {
                                    lock (patchedLock)
                                    {
                                        patchedProducts++;
                                    }
                                }
                            }


                        }
                        else
                        {
                            AddProductToList(product, "", newKeyword);
/*                            if(PatchProduct((string)product["id"], keywordsArray, "", newKeyword, token))
                            {
                                lock (patchedLock)
                                {
                                    patchedProducts++;
                                }
                            }*/
                        }
                    }
                }
            });
        }

        static string NewName(string prodName)
        {
            var symbolReplacements = new Dictionary<string, string>
                                    {
                                        { "¾", " 3/4" },
                                        { "½", " 1/2" },
                                        { "¼", " 1/4" },
                                        { "⅝", " 5/8" },
                                        { "⅜", " 3/8" },
                                        {"⅛", " 1/8" },
                                        {"⅞", " 7/8" },
                                        { "”", "\"" },
                                        { "“", "\"" },
                                        { "″", "\"" },
                                        {"–", "-" }, //Byte number 1 is decimal 150 *looks like normal dash but is not
                                        {"‑", "-" }, //&#8209 *looks like normal dash but is not
                                        {"−", "-" }, //&#8722 *looks like normal dash but is not
                                        { " ", " " }, //&#8200 *looks like normal space but is not
                                        { " ", " "}, //Byte number 1 is decimal 160 *looks like normal space but is not
                    //Use a UTF-8 decoder these aren't normal keyboard characters and therefor can't be easily searched for
                                    };
            string pattern = @"[¾¼½⅝⅜⅛⅞–‑−”“″  ]";
            string prodNameNoFractionSymbol = Regex.Replace(prodName, pattern, match =>
            {
                // Look up the replacement for the matched symbol
                if (symbolReplacements.TryGetValue(match.Value, out string replacement))
                {
                    return replacement;
                }
                // If no replacement is found, return the original match
                return match.Value;
            });
            //removes leading and trailing whitespace
            prodNameNoFractionSymbol = prodNameNoFractionSymbol.Trim();
            //removes consecutive whitespaces
            return Regex.Replace(prodNameNoFractionSymbol, @"\s+", " ");
        }

        static string NewKeyword(string prodName)
        {
            var symbolReplacements = new Dictionary<string, string>
                                    {
                                        { "¾", " 3/4" },
                                        { "½", " 1/2" },
                                        { "¼", " 1/4" },
                                        { "⅝", " 5/8" },
                                        { "⅜", " 3/8" },
                                        {"⅛", " 1/8" },
                                        {"⅞", " 7/8" },
                                        { "”", "\"" },
                                        { "“", "\"" },
                                        { "″", "\"" },
                                        {"–", " " }, //Byte number 1 is decimal 150 *looks like normal dash but is not
                                        {"‑", " " }, //&#8209 *looks like normal dash but is not
                                        {"−", " " }, //&#8722 *looks like normal dash but is not
                                        { " ", " " }, //&#8200 *looks like normal space but is not
                                        { " ", " "}, //Byte number 1 is decimal 160 *looks like normal space but is not
                    //Use a UTF-8 decoder these aren't normal keyboard characters and therefor can't be easily searched for
                                        {"™", " " },
                                        {"©", " " },
                                        {"°", " " },
                                        {"®", " " }
                                    };
            string pattern = @"[¾¼½⅝⅜⅛⅞–‑−”“″  ™©°®]";

            string prodNameNoFractionSymbol = Regex.Replace(prodName, pattern, match =>
            {
                // Look up the replacement for the matched symbol
                if (symbolReplacements.TryGetValue(match.Value, out string replacement))
                {
                    return replacement;
                }
                // If no replacement is found, return the original match
                return match.Value;
            });


            string prodNameNoSymbol = Regex.Replace(prodNameNoFractionSymbol, @"[,()\-\[\]]", " ", RegexOptions.Compiled);
            //removes leading and trailing whitespace
            prodNameNoSymbol = prodNameNoSymbol.Trim();
            //removes consecutive whitespaces
            return Regex.Replace(prodNameNoSymbol, @"\s+", " ");
        }

        static Boolean PatchProduct(string id, string[] curKeywords, string newName, string newKeyword, string token)
        {
            string[] newKeywords = new string[curKeywords.Length+1];
            curKeywords.CopyTo(newKeywords, 0);
            newKeywords[newKeywords.Length-1] = newKeyword;

            using (HttpClient client = new HttpClient())
            {
                //it is unlikely but this handles a timeout or rate limit on single request
                HttpResponseMessage responseMes = null;
                Boolean timeOut = true;
                Int32 sleepTime = 100;
                Int32 maxSleepTime = 10000;
                while (timeOut)
                {
                    try
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Patch, Settings.Products + "(" + id + ")");
                        request.Headers.Add("authorization", "Bearer " + token);
                        request.Headers.Add("accept", "*/*");
                        object jsonObject = null;
                        if (newName != "") {
                            jsonObject = new
                            {
                                metaKeywords = string.Join(", ", newKeywords),
                                shortDescription = newName
                            };
                        }
                        else
                        {
                            jsonObject = new
                            {
                                metaKeywords = string.Join(", ", newKeywords)
                            };
                        }
                        request.Content = new StringContent(JsonConvert.SerializeObject(jsonObject));
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        responseMes = client.Send(request);
                        responseMes.EnsureSuccessStatusCode();
                        string trashValue = responseMes.Content.ReadAsStringAsync().Result;
                        timeOut = false;
                        return true;
                    }
                    catch (Exception e)
                    {
                        //catch 401 for unauthorized meaning token has expired
                        if ((int)responseMes.StatusCode == 401)
                        {
                            Console.WriteLine("Token Expired, requesting new");
                            token = GetToken();
                        }
                        else
                        {
                            if (sleepTime > maxSleepTime)
                            {
                                Console.WriteLine("Fatal Error: API unresponsive");
                                Environment.Exit(1);
                            }
                            timeOut = true;
                            Console.Write("Product Update Failed: ");
                            Console.WriteLine(e.Message);
                            Console.WriteLine("Retrying in " + (sleepTime / 1000).ToString() + "seconds");
                            Thread.Sleep(sleepTime);
                            sleepTime *= 2;
                        }
                    }

                }
            }
            return false;
        }

        static void AddProductToList(JToken product, string newName, string newKeyword)
        {
            SpreadsheetInfoStruct spreadsheetInfoStruct = new SpreadsheetInfoStruct();
            spreadsheetInfoStruct.erpNumber = (string)product["erpNumber"];
            spreadsheetInfoStruct.id = (string)product["id"];
            spreadsheetInfoStruct.name = (string)product["shortDescription"];
            spreadsheetInfoStruct.newName = newName;
            spreadsheetInfoStruct.newKeyword = newKeyword;
            lock (flaggedLock)
            {
                if (!newName.Equals(""))
                {
                    newNameCounter++;
                }
                flagged++;
                flaggedProducts.Add(JObject.Parse(JsonConvert.SerializeObject(spreadsheetInfoStruct)));
            }
        }

        static JToken QueryAPIAllProducts(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage responseMes = null;
                string response = "";
                Boolean timeOut = true;
                //time in ms will double every failure
                Int32 sleepTime = 10000;
                while (timeOut)
                {
                    try
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Settings.Products + "?=&archiveFilter=0&$count=true&$orderby=erpNumber&$select=id,erpNumber,metaKeywords,shortDescription");
                        request.Headers.Add("authorization", "Bearer " + token);
                        responseMes = client.Send(request);
                        responseMes.EnsureSuccessStatusCode();
                        response = responseMes.Content.ReadAsStringAsync().Result;
                        timeOut = false;
                    }
                    catch (Exception ex)
                    {
                        timeOut = true;
                        Thread.Sleep(sleepTime);
                        sleepTime *= 2;
                        Console.WriteLine(ex.Message);
                        Console.WriteLine("Retrying Product Call");
                        Console.WriteLine("Sleeping for " + sleepTime.ToString());
                    }
                }


                if (response != null)
                {
                    if (responseMes.IsSuccessStatusCode)
                    {
                        JObject productsJSON = JObject.Parse(response);
                        Console.WriteLine("Found " + productsJSON["@odata.count"] + " Products");
                        return productsJSON["value"];
                    }
                }

                return null;
            }
        }

        private static string GetToken()
        {
            IdentityStruct identStruct = new IdentityStruct();
            identStruct.grant_type = "password";
            identStruct.username = "admin_username";
            identStruct.password = "password";
            identStruct.scope = "isc_admin_api offline_access";

            using (HttpClient client = new HttpClient())
            {
                //it is unlikely but this handles a timeout or rate limit on single token request
                HttpResponseMessage responseMes = null;
                Boolean timeOut = true;
                Int32 sleepTime = 100;
                Int32 maxSleepTime = 10000;
                while (timeOut)
                {
                    try
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Settings.identURL);
                        request.Headers.Add("authorization", Settings.authForToken);
                        request.Content = new StringContent(identStruct.ToString());
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        responseMes = client.Send(request);
                        responseMes.EnsureSuccessStatusCode();
                        JObject JSON = JObject.Parse(responseMes.Content.ReadAsStringAsync().Result);
                        string token = (string)JSON["access_token"];
                        timeOut = false;
                        return token;
                    }
                    catch (Exception e)
                    {
                        if ((int)responseMes.StatusCode == 400)
                        {
                            Console.WriteLine("Incorrect Credentials");
                            Environment.Exit(1);
                        }
                        if (sleepTime > maxSleepTime)
                        {
                            Console.WriteLine("Fatal Error: API unresponsive");
                            Environment.Exit(1);
                        }
                        timeOut = true;
                        Console.Write("Token Request Failed: ");
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Retrying in " + (sleepTime / 1000).ToString() + "seconds");
                        Thread.Sleep(sleepTime);
                        sleepTime *= 2;
                    }

                }
                return null;
            }
        }
    }
}
