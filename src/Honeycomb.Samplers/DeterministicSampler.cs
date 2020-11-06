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
        private const int AlwaysSample = 1;
        private const int NeverSample = 0;
        private const int IndexZero = 0;
        private const int IndexFour = 4;
        private const int Base16 = 16;
        private readonly SHA1 sha1 = SHA1.Create();

        public int SampleRate { get; private set; }
        public long UpperBound { get; private set; }

        public DeterministicSampler(int sampleRate)
        {
            if (sampleRate < NeverSample)
            {
                throw new ArgumentOutOfRangeException("Sample rate must not be negative.");
            }

            this.SampleRate = sampleRate;
            this.Description = string.Format(DescriptionFormat, this.SampleRate.ToString(CultureInfo.InvariantCulture));
            this.UpperBound = sampleRate == NeverSample ? NeverSample : uint.MaxValue / sampleRate;
        }

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            if (SampleRate == AlwaysSample)
            {
                return CreateResult(SamplingDecision.RecordAndSample, AlwaysSample);
            }
            if (SampleRate == NeverSample)
            {
                return CreateResult(SamplingDecision.Drop, NeverSample);
            }

            var bytes = Encoding.UTF8.GetBytes(samplingParameters.TraceId.ToString());
            var hash = sha1.ComputeHash(bytes);
            var determinant = Convert.ToUInt32(BitConverter.ToString(hash, IndexZero, IndexFour).Replace(Hyphen, string.Empty).ToLower(), Base16);
            var decision = determinant <= UpperBound
                ? SamplingDecision.RecordAndSample
                : SamplingDecision.Drop;

            return CreateResult(
                decision,
                decision == SamplingDecision.RecordAndSample ? SampleRate : NeverSample
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
