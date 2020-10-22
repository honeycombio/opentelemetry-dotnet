using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using OpenTelemetry.Trace;

namespace Honeycomb.Samplers.Test
{
    public class DeterministicSamplerTests
    {
        [Fact]
        public void Negative_samplerate_throws_exception()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeterministicSampler(-1));
        }

        [Fact]
        public void Description_includes_sample_rate()
        {
            Assert.Equal("DeterministicSampler(10)", new DeterministicSampler(10).Description);
        }

        [Theory]
        [InlineData("6cbf4d3e8a69f640a6db7d0ca66861c7", true)]
        [InlineData("31cbd4a6e54d33439ad887d2d90dce9e", false)]
        public void Sampler_works_with_given_sample_point(string traceId, bool isSampled)
        {
            SamplingResult result;
            var sampler = new DeterministicSampler(17); // sample 1 in 17

            result = sampler.ShouldSample(new SamplingParameters(new ActivityContext(), ActivityTraceId.CreateFromString(traceId), "span_name", ActivityKind.Server));
            Assert.Equal(isSampled ? SamplingDecision.RecordAndSample : SamplingDecision.Drop, result.Decision);
            Assert.Equal(new Dictionary<string, object> {{"sampleRate", isSampled ? 17 : 0 }}, result.Attributes);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(10)]
        [InlineData(20)]
        public void Check_sample_rates_are_within_expected_bounds(int sampleRate)
        {
            const double marginOfError = 0.05;
            const int sampleSize = 50000;
            var sampler = new DeterministicSampler(sampleRate);
            var count = 0;
            for (int i = 0; i < sampleSize; i++)
            {
                var result = sampler.ShouldSample(new SamplingParameters(new ActivityContext(), ActivityTraceId.CreateRandom(), "span_name", ActivityKind.Server));
                if (result.Decision == SamplingDecision.RecordAndSample)
                {
                    count++;
                }
            }

            var expectedSampleCount = sampleSize * (1 / (double) sampleRate);
            var variance = expectedSampleCount * marginOfError;
            Assert.InRange(count, expectedSampleCount - variance, expectedSampleCount + variance);
        }

        [Fact]
        public void Sample_rate_of_1_samples_all()
        {
            var sampler = new DeterministicSampler(1);
            var count = 0;
            for (int i = 0; i < 50000; i++)
            {
                var result = sampler.ShouldSample(new SamplingParameters(new ActivityContext(), ActivityTraceId.CreateRandom(), "span_name", ActivityKind.Server));
                if (result.Decision == SamplingDecision.RecordAndSample)
                {
                    count++;
                }
            }

            Assert.Equal(50000, count);
        }

        [Fact]
        public void Sample_rate_of_0_samples_none()
        {
            var sampler = new DeterministicSampler(0);
            var count = 0;
            for (int i = 0; i < 50000; i++)
            {
                var result = sampler.ShouldSample(new SamplingParameters(new ActivityContext(), ActivityTraceId.CreateRandom(), "span_name", ActivityKind.Server));
                if (result.Decision == SamplingDecision.RecordAndSample)
                {
                    count++;
                }
            }

            Assert.Equal(0, count);
        }

        [Fact]
        public void Verify_samplers_give_consistent_results()
        {
            var samplerA = new DeterministicSampler(3);
            var samplerb = new DeterministicSampler(3);
            var samplingParams = new SamplingParameters(new ActivityContext(), ActivityTraceId.CreateRandom(), "span_name", ActivityKind.Server);
            var firstAnswer = samplerA.ShouldSample(samplingParams);

            for (int i = 0; i < 25; i++)
            {
                var resultA = samplerA.ShouldSample(samplingParams);
                Assert.Equal(firstAnswer.Decision, resultA.Decision);

                var resultB = samplerb.ShouldSample(samplingParams);
                Assert.Equal(firstAnswer.Decision, resultB.Decision);
            }
        }
    }
}
