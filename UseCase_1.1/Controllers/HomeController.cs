using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace UseCase_1._1.Controllers
{
    public class HomeController : Controller
    {
        const string API_KEY = "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";
        private readonly ILogger<HomeController> _logger;
        static readonly HttpClient client = new HttpClient();

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Create()
        {

            ViewBag.Input = "No input data available."; // Initialize the ViewBag
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                // Read input from the uploaded file
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    var input = await reader.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(input))
                    {
                        // Save the input to a TempData variable
                        TempData["UploadedFileContent"] = input;
                        return Content(input); // Return the file content as plain text
                    }
                }
            }

            return BadRequest("Invalid file or no content.");
        }


        [HttpPost]
        public async Task<IActionResult> Get(string prompt, string file)
        {
            try
            {
                if (string.IsNullOrEmpty(prompt))
                {
                    return BadRequest("Invalid prompt.");
                }

                // Append the content of the uploaded file to the prompt
                if (!string.IsNullOrEmpty(file))
                {
                    var uploadedFileContent = TempData["UploadedFileContent"] as string;
                    if (!string.IsNullOrEmpty(uploadedFileContent))
                    {
                        prompt += ": " + uploadedFileContent;
                    }
                }

                // Construct the chatbot message options
                var options = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = ""
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    max_tokens = 3500,
                    temperature = 0.2
                };

                var json = JsonConvert.SerializeObject(options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                string output = jsonResponse.choices[0].message.content;

                return Ok(output); // Return the response as text
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public IActionResult DownloadFile(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return NotFound("Response not found.");
            }

            byte[] fileBytes = Encoding.UTF8.GetBytes(response);
            return File(fileBytes, "text/plain", $"Chatbot_Response_{DateTime.Now:yyyyMMddHHmmss}.txt");
        }
    }
}