using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AzureCpfValidator
{
    public static class ValidarCpf
    {
        [FunctionName("ValidarCpf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Validando CPF");

            if (req.Body.Length > 200)
            {
                var response = "Corpo da requisição superior a 200 bytes, abortando requisição";
                log.LogInformation(response);
                return new BadRequestObjectResult(response);
            }

            using var reader = new StreamReader(req.Body);
            var payload = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(payload))
            {
                var response = "Corpo da requisição é invalido!";
                log.LogInformation(response);
                return new BadRequestObjectResult(response);
            }

            var isValid = CheckIfCpfIsValid(payload);

            log.LogInformation("CPF é: {0}", isValid ? "Valido" : "Invalido");
            return isValid ?
                new OkObjectResult("CPF Valido!")
                : new BadRequestObjectResult("CPF Invalido!");
        }

        private static bool CheckIfCpfIsValid(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
            {
                return false;
            }

            cpf = new string(cpf.Where(char.IsDigit).ToArray()); // Remove caracteres não numéricos

            // CPF não pode ter todos os dígitos iguais
            if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            {
                return false;
            }

            int[] firstMultiplier = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] secondMultiplier = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf[..9];
            int sum = 0;

            for (int i = 0; i < 9; i++)
            {
                sum += (tempCpf[i] - '0') * firstMultiplier[i];
            }

            int remaining = sum % 11;
            int firstDigit = remaining < 2 ? 0 : 11 - remaining;

            tempCpf += firstDigit;
            sum = 0;

            for (int i = 0; i < 10; i++)
            {
                sum += (tempCpf[i] - '0') * secondMultiplier[i];
            }

            remaining = sum % 11;
            int secondDigit = remaining < 2 ? 0 : 11 - remaining;

            return cpf.EndsWith(firstDigit.ToString() + secondDigit.ToString());
        }
    }
}
