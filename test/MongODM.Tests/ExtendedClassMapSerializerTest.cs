using Digicando.DomainHelper;
using Digicando.MongODM.Comparers;
using Digicando.MongODM.Models;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Digicando.MongODM
{
    public class ExtendedClassMapSerializerTest
    {
        // Internal classes.
        public class DeserializationTestElement
        {
            public DeserializationTestElement(
                Action<BsonReader> bsonReaderPreAction = null,
                Action<BsonReader> bsonReaderPostAction = null)
            {
                BsonReaderPreAction = bsonReaderPreAction ?? (rd => { });
                BsonReaderPostAction = bsonReaderPostAction ?? (rd => { });
            }

            public Action<BsonReader> BsonReaderPostAction { get; }
            public Action<BsonReader> BsonReaderPreAction { get; }
            public BsonDocument Document { get; set; }
            public FakeModel ExpectedModel { get; set; }
        }
        public class SerializationTestElement
        {
            public SerializationTestElement(
                Action<BsonWriter> bsonWriterPreAction = null,
                Action<BsonWriter> bsonWriterPostAction = null)
            {
                SerializedDocument = new BsonDocument();
                BsonWriter = new BsonDocumentWriter(SerializedDocument);
                BsonWriterPreAction = bsonWriterPreAction ?? (wr => { });
                BsonWriterPostAction = bsonWriterPostAction ?? (wr => { });
            }

            public BsonWriter BsonWriter { get; }
            public Action<BsonWriter> BsonWriterPostAction { get; }
            public Action<BsonWriter> BsonWriterPreAction { get; }
            public Func<BsonSerializationContext, bool> Condition { get; set; }
            public BsonDocument ExpectedDocument { get; set; }
            public FakeModel Model { get; set; }
            public BsonDocument SerializedDocument { get; }
        }

        // Fields.
        private readonly Mock<IDbContext> dbContextMock;
        private readonly DocumentVersion documentVersion = new DocumentVersion("1.0.0");
        private readonly Mock<ISerializerModifierAccessor> serializerModifierAccessorMock;

        // Constructor.
        public ExtendedClassMapSerializerTest()
        {
            dbContextMock = new Mock<IDbContext>();
            dbContextMock.Setup(c => c.DocumentVersion)
                .Returns(() => documentVersion);
            dbContextMock.Setup(c => c.DBCache.LoadedModels.ContainsKey(It.IsAny<object>()))
                .Returns(() => false);

            serializerModifierAccessorMock = new Mock<ISerializerModifierAccessor>();
        }

        // Tests.
        public static IEnumerable<object[]> DeserializationTests
        {
            get
            {
                var tests = new List<DeserializationTestElement>
                {
                    // Null model
                    new DeserializationTestElement(
                        rd =>
                        {
                            rd.ReadStartDocument();
                            rd.ReadName();
                        },
                        rs => rs.ReadEndDocument())
                    {
                        ExpectedModel = null,
                        Document = new BsonDocument(new BsonElement("elem", BsonNull.Value))
                    },

                    // Model without extra members
                    new DeserializationTestElement
                    {
                        ExpectedModel = new FakeModel
                        {
                            Id = "idVal",
                            IntegerProp = 8,
                            StringProp = "ok"
                        },
                        Document = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("IntegerProp", new BsonInt32(8)),
                            new BsonElement("StringProp", new BsonString("ok"))
                        } as IEnumerable<BsonElement>)
                    },

                    // Model with extra members
                    new DeserializationTestElement
                    {
                        ExpectedModel = new FakeModel
                        {
                            Id = "idVal",
                            IntegerProp = 8,
                            StringProp = "rightValue"
                        },
                        Document = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("IntegerProp", new BsonInt32(8)),
                            new BsonElement("StringProp", new BsonString("wrongValue")),
                            new BsonElement("ExtraElement", new BsonString("rightValue"))
                        } as IEnumerable<BsonElement>)
                    }
                };
                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(DeserializationTests))]
        public void Deserialize(DeserializationTestElement test)
        {
            // Setup
            var bsonReader = new BsonDocumentReader(test.Document);
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbContextMock.Object,
                serializerModifierAccessorMock.Object,
                (m, _) =>
                {
                    if (m.ExtraElements != null &&
                        m.ExtraElements.ContainsKey("ExtraElement"))
                        m.StringProp = m.ExtraElements["ExtraElement"] as string;
                    ReflectionHelper.SetValue(m, m1 => m1.ExtraElements, null);
                    return Task.FromResult(m);
                });

            // Action
            test.BsonReaderPreAction(bsonReader);
            var result = serializer.Deserialize(
                BsonDeserializationContext.CreateRoot(bsonReader),
                new BsonDeserializationArgs { NominalType = typeof(FakeModel) });
            test.BsonReaderPostAction(bsonReader);

            // Assert
            Assert.Equal(test.ExpectedModel, result as FakeModel, new FakeModelComparer());
        }

        [Fact]
        public void GetDocumentId()
        {
            // Setup
            var model = new FakeModel { Id = "idVal" };
            var comparisonSerializer = CreateBsonClassMapSerializer();
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbContextMock.Object,
                serializerModifierAccessorMock.Object);

            // Action
            var result = serializer.GetDocumentId(
                model,
                out object idResult,
                out Type idNominalTypeResult,
                out IIdGenerator idGeneratorResult);

            // Assert
            var resultExpected = comparisonSerializer.GetDocumentId(
                model,
                out object idExpected,
                out Type idNominalTypeExpected,
                out IIdGenerator idGeneratorExpected);

            Assert.Equal(idExpected, idResult);
            Assert.Equal(idNominalTypeExpected, idNominalTypeResult);
            Assert.Equal(idGeneratorExpected, idGeneratorResult);
            Assert.Equal(resultExpected, result);
        }

        [Fact]
        public void GetMemberSerializationInfo()
        {
            // Setup
            var memberName = nameof(FakeModel.StringProp);
            var comparisonSerializer = CreateBsonClassMapSerializer();
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbContextMock.Object,
                serializerModifierAccessorMock.Object);

            // Action
            var result = serializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo serializationInfo);

            // Assert
            var expectedResult = comparisonSerializer.TryGetMemberSerializationInfo(memberName,
                out BsonSerializationInfo expectedSerializationInfo);
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedSerializationInfo.ElementName, serializationInfo.ElementName);
            Assert.Equal(expectedSerializationInfo.NominalType, serializationInfo.NominalType);
        }

        public static IEnumerable<object[]> SerializationTests
        {
            get
            {
                var tests = new List<SerializationTestElement>
                {
                    // Null model
                    new SerializationTestElement(
                        wr =>
                        {
                            wr.WriteStartDocument();
                            wr.WriteName("elem");
                        },
                        wr => wr.WriteEndDocument())
                    {
                        Model = null,
                        ExpectedDocument = new BsonDocument(new BsonElement("elem", BsonNull.Value))
                    },

                    // Complex model
                    new SerializationTestElement
                    {
                        Model = new FakeModel()
                        {
                            EnumerableProp = new[] { new FakeModel(), null },
                            Id = "idVal",
                            IntegerProp = 42,
                            ObjectProp = new FakeModel(),
                            StringProp = "yes"
                        },
                        ExpectedDocument = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", new BsonArray(new BsonValue[]
                            {
                                new BsonDocument(new BsonElement[]
                                {
                                    new BsonElement("_id", BsonNull.Value),
                                    new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                    new BsonElement("EnumerableProp", BsonNull.Value),
                                    new BsonElement("IntegerProp", new BsonInt32(0)),
                                    new BsonElement("ObjectProp", BsonNull.Value),
                                    new BsonElement("StringProp", BsonNull.Value)
                                } as IEnumerable<BsonElement>),
                                BsonNull.Value
                            })),
                            new BsonElement("IntegerProp", new BsonInt32(42)),
                            new BsonElement("ObjectProp", new BsonDocument(new BsonElement[]
                            {
                                new BsonElement("_id", BsonNull.Value),
                                new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                new BsonElement("EnumerableProp", BsonNull.Value),
                                new BsonElement("IntegerProp", new BsonInt32(0)),
                                new BsonElement("ObjectProp", BsonNull.Value),
                                new BsonElement("StringProp", BsonNull.Value)
                            } as IEnumerable<BsonElement>)),
                            new BsonElement("StringProp", new BsonString("yes")),
                            new BsonElement("ExtraElement", new BsonString("extraValue"))
                        } as IEnumerable<BsonElement>)
                    },

                    // True condition.
                    new SerializationTestElement
                    {
                        Condition = _ => true,
                        Model = new FakeModel()
                        {
                            Id = "idVal",
                        },
                        ExpectedDocument = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", BsonNull.Value),
                            new BsonElement("IntegerProp", new BsonInt32(0)),
                            new BsonElement("ObjectProp", BsonNull.Value),
                            new BsonElement("StringProp", BsonNull.Value),
                            new BsonElement("ExtraElement", new BsonString("extraValue"))
                        } as IEnumerable<BsonElement>)
                    },

                    // False condition.
                    new SerializationTestElement
                    {
                        Condition = _ => false,
                        Model = new FakeModel()
                        {
                            Id = "idVal",
                        },
                        ExpectedDocument = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", BsonNull.Value),
                            new BsonElement("IntegerProp", new BsonInt32(0)),
                            new BsonElement("ObjectProp", BsonNull.Value),
                            new BsonElement("StringProp", BsonNull.Value)
                        } as IEnumerable<BsonElement>)
                    }
                };

                // With a proxy class.
                {
                    var model = new FakeModelProxy()
                    {
                        Id = "idVal",
                        IntegerProp = 42,
                        ObjectProp = new FakeModel(),
                        StringProp = "yes"
                    };
                    tests.Add(new SerializationTestElement
                    {
                        Condition = _ => false,
                        Model = model,
                        ExpectedDocument = new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", BsonNull.Value),
                            new BsonElement("IntegerProp", new BsonInt32(42)),
                            new BsonElement("ObjectProp", new BsonDocument(new BsonElement[]
                            {
                                new BsonElement("_id", BsonNull.Value),
                                new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                new BsonElement("EnumerableProp", BsonNull.Value),
                                new BsonElement("IntegerProp", new BsonInt32(0)),
                                new BsonElement("ObjectProp", BsonNull.Value),
                                new BsonElement("StringProp", BsonNull.Value)
                            } as IEnumerable<BsonElement>)),
                            new BsonElement("StringProp", new BsonString("yes"))
                        } as IEnumerable<BsonElement>)
                    });
                }

                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(SerializationTests))]
        public void Serialize(SerializationTestElement test)
        {
            // Setup
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbContextMock.Object,
                serializerModifierAccessorMock.Object)
                .AddExtraElement(new BsonElement("ExtraElement", new BsonString("extraValue")), test.Condition);

            // Action
            test.BsonWriterPreAction(test.BsonWriter);
            serializer.Serialize(
                BsonSerializationContext.CreateRoot(test.BsonWriter),
                new BsonSerializationArgs { NominalType = typeof(FakeModel) },
                test.Model);
            test.BsonWriterPostAction(test.BsonWriter);

            // Assert
            Assert.Equal(0, test.SerializedDocument.CompareTo(test.ExpectedDocument));
        }

        [Fact]
        public void SetDocumentId()
        {
            // Setup
            var id = "idVal";
            var model = new FakeModel();
            var comparisonSerializer = CreateBsonClassMapSerializer();
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbContextMock.Object,
                serializerModifierAccessorMock.Object);

            // Action
            serializer.SetDocumentId(model, id);

            // Assert
            var expectedModel = new FakeModel();
            comparisonSerializer.SetDocumentId(expectedModel, id);
            Assert.Equal(expectedModel, model, new FakeModelComparer());
        }

        private static BsonClassMapSerializer<FakeModel> CreateBsonClassMapSerializer()
        {
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            return new BsonClassMapSerializer<FakeModel>(classMap);
        }
    }
}
