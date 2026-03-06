using Worker.Abstractions;
using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Worker.Service
{
    public class Executor : IExecutor
    {

        private readonly ILogger<Executor> _logger;

        public Executor(ILogger<Executor> logger)
        {
            _logger = logger;
        }

        public Task<CrackHashWorkerResponse> Execute(CrackHashManagerRequest request)
        {

            var generator = new ByteBruteforceGenerator(request.Alphabet);

            long count = (long)Math.Pow(request.Alphabet.Length, request.MaxLength);

            var variations = generator.GenerateForWorker(request.MaxLength, request.PartNumber, request.PartCount);

            using var md5 = MD5.Create();

            byte[] sourceHashBytes = HashToBytes(request.Hash);

            List<string> answers = [];

            _logger.LogInformation($"Ready to bruteforce for request: {request.RequestId}, {request.PartNumber}/{request.PartCount}.");

            var startTime = DateTime.Now;

            foreach (var item in variations)
            {
                var bytes = item;
                byte[] hash = md5.ComputeHash(bytes);
                bool sameHash = CompareHash(hash, sourceHashBytes);
                if (sameHash)
                {
                    var word = Encoding.UTF8.GetString(item);
                    _logger.LogInformation($"Found matching word '{word}' for request: {request.RequestId}, {request.PartNumber}/{request.PartCount}.");
                    answers.Add(word);
                }
            }

            var endTime = DateTime.Now;

            _logger.LogInformation($"Completed bruteforce for request: {request.RequestId}, {request.PartNumber}/{request.PartCount} for {endTime - startTime}");

            var result = new CrackHashWorkerResponse
            {
                RequestId = request.RequestId,
                PartNumber = request.PartNumber,
                Answers = answers.ToArray()
            };

            return Task.FromResult(result);
        }

        private byte[] HashToBytes(string hashString)
        {
            const int hashSize = 16;
            byte[] hashBytes = new byte[hashSize];

            var span = hashString.AsSpan();

            for (int i = 0; i < span.Length; i += 2)
            {
                byte b = byte.Parse(new string([span[i], span[i + 1]]), System.Globalization.NumberStyles.HexNumber);
                hashBytes[i / 2] = b;
            }
            
            return hashBytes;
        }

        private bool CompareHash(byte[] hash1, byte[] hash2)
        {
            return hash1.SequenceEqual(hash2);
        }


    }
}
