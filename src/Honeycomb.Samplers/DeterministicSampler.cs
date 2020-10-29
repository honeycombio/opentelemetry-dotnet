using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using OpenTelemetry.Trace;

namespace Honeycomb.Samplers
{
    public class DeterministicSampler : Sampler, IDisposable
    {
        private const string DescriptionFormat = "DeterministicSampler({0})";
        private const string SampleRateAttributeName = "sampleRate";
        private const string Hyphen = "-";
        private const int Zero = 0;
        private const int Four = 4;
        private const int One = 1;
        private const int Base16 = 16;
        private readonly SHA1 sha1 = SHA1.Create();
        private readonly int sampleRate;
        private readonly long upperBound;

        public DeterministicSampler(int sampleRate)
        {
            if (sampleRate < Zero)
            {
                throw new ArgumentOutOfRangeException("Sample rate must not be negative.");
            }

            this.sampleRate = sampleRate;
            this.Description = string.Format(DescriptionFormat, this.sampleRate.ToString(CultureInfo.InvariantCulture));
            this.upperBound = sampleRate == Zero ? Zero : uint.MaxValue / sampleRate;
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            if (sampleRate == One)
            {
                return CreateResult(SamplingDecision.RecordAndSample, One);
            }
            if (sampleRate == Zero)
            {
                return CreateResult(SamplingDecision.Drop, Zero);
            }

            var bytes = Encoding.UTF8.GetBytes(samplingParameters.TraceId.ToString());
            var hash = sha1.ComputeHash(bytes);
            var determinant = Convert.ToUInt32(BitConverter.ToString(hash, Zero, Four).Replace(Hyphen, string.Empty).ToLower(), Base16);
            var decision = determinant <= upperBound
                ? SamplingDecision.RecordAndSample
                : SamplingDecision.Drop;

            return CreateResult(
                decision,
                decision == SamplingDecision.RecordAndSample ? sampleRate : Zero
            );
        }

        private static SamplingResult CreateResult(SamplingDecision decision, int sampleRate)
        {
            return new SamplingResult(
                decision,
                new Dictionary<string, object>{ {SampleRateAttributeName, sampleRate} }
            );
        }

        public void Dispose()
        {
            sha1.Dispose();
        }
    }
}
