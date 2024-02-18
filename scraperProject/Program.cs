using HtmlAgilityPack;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace scraperProject
{
    class Program
    {
        //constants for changing background
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINFILE = 1;
        public const int SPIF_SENDCHANGE = 2;
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo   (int uAction, int uParam, string lpvParam, int fuWinIni);
        
        
        //we pass in a theme as a String
        static void Main(String[] args)
        {
            Console.WriteLine("Welcome to Image Extractor 9000!");

            var targetUrl = "";
            while (targetUrl != "STOP")
            {
                Console.WriteLine("Please enter the SpeedHunters URl to scrape (type STOP to exit!):");
                targetUrl = Console.ReadLine();
                if (targetUrl == "STOP")
                {
                    Console.WriteLine("Thank you for using the Image Extractor 9000!");
                    break;
                }
                targetUrl = "http://www.speedhunters.com/";
                //check if the folder has been updated within the last 
                var folder = "C:\\Users\\chewie\\Desktop\\test_images";
                bool renew = outDated(folder);

                //if folder was renewed within 7 days
                //pick random image
                if (renew)
                {
                    Console.WriteLine("Just picking random image");
                    setDesktop(folder);
                }
               
                else
                {
                    Console.WriteLine("getting new images");
                    //get a random article to scrape
                    string page = getArticle(targetUrl);
                    List<string> images = parseHtml(page);

                    //download the images to a directory
                    var done = downloadImages(folder, images);
                    //set image
                    setDesktop(folder);
                }
            }
        }

        //returns a boolean indicating if the images are outdated
        //returns true if images were modified within last 7 days
        //returns false otherwise
        private static bool outDated(string folder)
        {
            DateTime curr = DateTime.Now;
            var lastModified = System.IO.File.GetLastWriteTime(folder);
            var delta = curr - lastModified;
            Console.WriteLine(delta.ToString());
            if (delta.Days <= 7) { return true; }
            return false;
        }

        //method to request a url from a webpage
        private static async Task<string> requestPage(string url)
        {
            HttpClient client = new HttpClient();
            var response = await client.GetStringAsync(url);
            return response;
        }

        //parsing the HTML for img tags
        private static List<string> parseHtml(string html)
        {
            Console.WriteLine("Parsing HTML");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var links = doc.DocumentNode.Descendants("img").
                Where(node => node.GetAttributeValue("class", "").Contains("alignnone")).ToList();

            List<string> imgLinks = new List<string>();
            foreach (var link in links)
            {
                /*
                for(var a = 0; a < link.Attributes.Count; a++)
                {
                    //Console.WriteLine(link.Attributes[a].Name + " : " + link.Attributes[a].Value);
                }
                */
                //this is the url to the image
                //.Single() allows us to select the first occurence of an element that matches our condition.
                var data = link.Attributes.Single(att => att.Name == "data-go-fullscreen").Value;
                imgLinks.Add(data);
            }
            return imgLinks;
        }
        private static int getRandom(int max)
        {
            Random random = new Random();
            return random.Next(0, max);
        }

        //only call this when the current folder is empty
        //OR NEW CARS BUTTON SELECTED
        //find a random article to scrape
        //for rn its a default site of speedhunters
        private static string getArticle(string target)
        {
            Console.WriteLine("Getting random article");
            //we have a good URl so we make a request and store the response
            string page = requestPage(target).Result;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);

            var articles = doc.DocumentNode.Descendants("a").
                Where(node => node.GetAttributeValue("class", "").Contains("content-thumbnail")).ToList();

            //selecting our random article and its link 
            int tar = getRandom(articles.Count);
            var data = articles[tar].Attributes[1].Value;
            
            return requestPage(data).Result;
        }
        
        //downloading the image from the URL
        private static async Task<bool> downloadImages(string filePath, List<string> links)
        {
            var HttpClient = new HttpClient();
            if (!Directory.Exists(filePath))
            {
                Console.WriteLine($"created a new directory at {filePath}");
                Directory.CreateDirectory(filePath);
            }
            else
            {
                Console.WriteLine($"{filePath} exists!");
                //only get new image 
                DateTime lastModified = Directory.GetLastWriteTime(filePath);
            }
            for (var i = 0; i < links.Count; i++) 
            {
                var fileName = "image" + i + ".jpg";

                var imageBytes = await HttpClient.GetByteArrayAsync(links[i]);
                var path = Path.Combine(filePath, $"{fileName}");
                await File.WriteAllBytesAsync(path, imageBytes);
            }
            return true;
        }

        //update the background image
        //pick a random one from the availables
        //delete that photo afterwards
        public static void setDesktop(String folderPath) {
            Console.WriteLine("Setting Desktop Wallpaper");
            //getting a random image and its path
            int fileCount = Directory.GetFiles(folderPath, "*.jpg", SearchOption.TopDirectoryOnly).Length;
            int index = getRandom(fileCount);
            string imagePath = folderPath + @"\image"+index+".jpg";
            //updating the desktop
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINFILE);
            //deleting the image afterwards
            File.Delete(imagePath);
        }
    }
}
