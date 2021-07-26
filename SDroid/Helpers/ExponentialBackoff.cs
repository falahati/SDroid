using System;
using System.Threading.Tasks;

namespace SDroid.Helpers
{
    public class ExponentialBackoff
    {
        private readonly double _backOffFactor;

        public ExponentialBackoff(double backOffFactor = 1)
        {
            _backOffFactor = backOffFactor;
        }

        public int Attempts { get; set; }

        public async Task Delay()
        {
            var delay = Math.Max(Math.Min((Math.Pow(2, Attempts) - 1) * _backOffFactor, 60 * 60), 0);

            if (delay > 0.016)
            {
                await Task.Delay(TimeSpan.FromSeconds(delay)).ConfigureAwait(false);
            }

            Attempts++;
        }

        public void Reset()
        {
            Attempts = 0;
        }
    }
}