using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace PleskManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubdomainController : ControllerBase
    {
        private readonly ILogger<SubdomainController> _logger;

        public SubdomainController(ILogger<SubdomainController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult CreateSubdomain(string Subdomain)
        {
            try
            {
                _logger.LogInformation($"[START] Subdomain creation process for {Subdomain}.sabilwater.com");

                // Create Subdomain
                string createSubdomainCommand = $"sudo plesk bin subdomain --create {Subdomain} -domain sabilwater.com -www-root {Subdomain}";
                _logger.LogInformation($"Executing command: {createSubdomainCommand}");
                var createSubdomainResult = ExecuteCommand(createSubdomainCommand);
                _logger.LogInformation($"Subdomain Creation Result: {createSubdomainResult}");
                Thread.Sleep(2000);
                // Configure SSL
                string sslCommand = $"sudo plesk bin extension --exec letsencrypt cli.php --domain {Subdomain}.sabilwater.com --email newin386@gmail.com --agree-tos --letsencrypt-ssl";
                _logger.LogInformation($"Executing command: {sslCommand}");
                var sslResult = ExecuteCommand(sslCommand);
                _logger.LogInformation($"SSL Configuration Result: {sslResult}");
                Thread.Sleep(2000);
                // Update Nginx Configuration
                string nginxConfigPath = $"/etc/nginx/plesk.conf.d/vhosts/{Subdomain}.sabilwater.com.conf";
                string updateNginxCommand = @"sudo sed -i '/location \//,/}/c\location / {\n    proxy_pass http://localhost:3000;\n    proxy_hide_header upgrade;\n    proxy_set_header Host             \$host;\n    proxy_set_header X-Real-IP        \$remote_addr;\n    proxy_set_header X-Forwarded-For  \$proxy_add_x_forwarded_for;\n    proxy_set_header X-Accel-Internal /internal-nginx-static-location;\n    access_log off;\n}' " + nginxConfigPath;
                _logger.LogInformation($"Executing command: {updateNginxCommand}");
                var updateNginxConfigResult = ExecuteCommand(updateNginxCommand);
                _logger.LogInformation($"Nginx Config Update Result: {updateNginxConfigResult}");
                Thread.Sleep(2000);
                // Restart Nginx
                var restartNginxResult = ExecuteCommand("echo Password@123 | sudo -S systemctl restart nginx");
                _logger.LogInformation($"Nginx Restart Result: {restartNginxResult}");

                _logger.LogInformation($"[END] Subdomain creation process for {Subdomain}.sabilwater.com");

                return Ok(new
                {
                    SubdomainResult = createSubdomainResult,
                    SslResult = sslResult,
                    NginxConfigResult = updateNginxConfigResult,
                    NginxRestartResult = restartNginxResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during subdomain creation.");
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult GetSubDomain()
        {
            return Ok(new { Name = "KaramElSham", ImageUrl = "https://avatars.githubusercontent.com/u/144709620?v=4" });
        }

        private string ExecuteCommand(string command)
        {
            _logger.LogInformation($"Starting command execution: {command}");
            var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,  // Capture errors
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                // Log or return the error for debugging
                _logger.LogError($"Error during command execution: {error}");
                output += $"\nError: {error}";
            }

            _logger.LogInformation($"Command execution completed with output: {output}");
            return output;
        }
    }
}
