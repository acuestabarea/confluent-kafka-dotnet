// Copyright 2016-2017 Confluent Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Refer to LICENSE for more information.

#pragma warning disable xUnit1026

using Confluent.Kafka.Serdes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;


namespace Confluent.Kafka.IntegrationTests
{
    public partial class Tests
    {
        /// <summary>
        ///     Basic test of Consumer.Seek.
        /// </summary>
        [Theory, MemberData(nameof(KafkaParameters))]
        public void Consumer_Seek(string bootstrapServers)
        {
            LogToFile("start Consumer_Seek");

            var consumerConfig = new ConsumerConfig
            {
                GroupId = Guid.NewGuid().ToString(),
                BootstrapServers = bootstrapServers
            };

            var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };

            using (var producer = new ProducerBuilder<byte[], byte[]>(producerConfig).Build())
            using (var consumer =
                new ConsumerBuilder<Null, string>(consumerConfig)
                    .SetErrorHandler((_, e) => Assert.True(false, e.Reason))
                    .Build())
            {
                const string checkValue = "check value";
                var dr = producer.ProduceAsync(singlePartitionTopic, new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize(checkValue, SerializationContext.Empty) }).Result;
                var dr2 = producer.ProduceAsync(singlePartitionTopic, new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize("second value", SerializationContext.Empty) }).Result;
                var dr3 = producer.ProduceAsync(singlePartitionTopic, new Message<byte[], byte[]> { Value = Serializers.Utf8.Serialize("third value", SerializationContext.Empty) }).Result;

                consumer.Assign(new TopicPartitionOffset[] { new TopicPartitionOffset(singlePartitionTopic, 0, dr.Offset) });

                var record = consumer.Consume(TimeSpan.FromSeconds(10));
                Assert.NotNull(record.Message);
                record = consumer.Consume(TimeSpan.FromSeconds(10));
                Assert.NotNull(record.Message);
                record = consumer.Consume(TimeSpan.FromSeconds(10));
                Assert.NotNull(record.Message);
                consumer.Seek(dr.TopicPartitionOffset);

                // position is that of the last consumed offset. it shouldn't be equal to the seek position.
                var pos = consumer.Position(new List<TopicPartition> { dr.TopicPartition }).First();
                Assert.NotEqual(dr.Offset, pos.Offset);

                record = consumer.Consume(TimeSpan.FromSeconds(10));
                Assert.NotNull(record.Message);
                Assert.Equal(checkValue, record.Message.Value);
            }

            Assert.Equal(0, Library.HandleCount);
            LogToFile("end   Consumer_Seek");
        }

    }
}
