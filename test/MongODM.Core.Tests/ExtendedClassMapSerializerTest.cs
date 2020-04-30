using Digicando.DomainHelper;
using Digicando.MongODM.Comparers;
using Digicando.MongODM.Models;
using Digicando.MongODM.Serialization;
using Digicando.MongODM.Serialization.Modifiers;
using Digicando.MongODM.Serialization.Serializers;
using Digicando.MongODM.Utility;
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
                BsonDocument document,
                FakeModel? expectedModel,
                Action<BsonReader>? preAction = null,
                Action<BsonReader>? postAction = null)
            {
                Document = document;
                ExpectedModel = expectedModel;
                PreAction = preAction ?? (rd => { });
                PostAction = postAction ?? (rd => { });
            }

            public BsonDocument Document { get; }
            public FakeModel? ExpectedModel { get; }
            public Action<BsonReader> PreAction { get; }
            public Action<BsonReader> PostAction { get; }
        }
        public class SerializationTestElement
        {
            public SerializationTestElement(
                FakeModel? model,
                BsonDocument expectedDocument,
                Func<BsonSerializationContext, bool>? condition = null,
                Action<BsonWriter>? preAction = null,
                Action<BsonWriter>? postAction = null)
            {
                BsonWriter = new BsonDocumentWriter(SerializedDocument);
                Condition = condition;
                ExpectedDocument = expectedDocument;
                Model = model;
                PreAction = preAction ?? (wr => { });
                PostAction = postAction ?? (wr => { });
            }

            public BsonWriter BsonWriter { get; }
            public Func<BsonSerializationContext, bool>? Condition { get; }
            public BsonDocument ExpectedDocument { get; }
            public FakeModel? Model { get; }
            public Action<BsonWriter> PreAction { get; }
            public Action<BsonWriter> PostAction { get; }
            public BsonDocument SerializedDocument { get; } = new BsonDocument();
        }

        // Fields.
        private readonly Mock<IDbCache> dbCacheMock;
        private readonly DocumentVersion documentVersion = new DocumentVersion("1.0.0");
        private readonly Mock<ISerializerModifierAccessor> serializerModifierAccessorMock;

        // Constructor.
        public ExtendedClassMapSerializerTest()
        {
            dbCacheMock = new Mock<IDbCache>();
            dbCacheMock.Setup(c => c.LoadedModels.ContainsKey(It.IsAny<object>()))
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
                        new BsonDocument(new BsonElement("elem", BsonNull.Value)),
                        null,
                        preAction: rd =>
                        {
                            rd.ReadStartDocument();
                            rd.ReadName();
                        },
                        postAction: rd => rd.ReadEndDocument()),

                    // Model without extra members
                    new DeserializationTestElement(
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("IntegerProp", new BsonInt32(8)),
                            new BsonElement("StringProp", new BsonString("ok"))
                        } as IEnumerable<BsonElement>),
                        new FakeModel
                        {
                            Id = "idVal",
                            IntegerProp = 8,
                            StringProp = "ok"
                        }),

                    // Model with extra members
                    new DeserializationTestElement(
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("IntegerProp", new BsonInt32(8)),
                            new BsonElement("StringProp", new BsonString("wrongValue")),
                            new BsonElement("ExtraElement", new BsonString("rightValue"))
                        } as IEnumerable<BsonElement>),
                        new FakeModel
                        {
                            Id = "idVal",
                            IntegerProp = 8,
                            StringProp = "rightValue"
                        })
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
                dbCacheMock.Object,
                documentVersion,
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
            test.PreAction(bsonReader);
            var result = serializer.Deserialize(
                BsonDeserializationContext.CreateRoot(bsonReader),
                new BsonDeserializationArgs { NominalType = typeof(FakeModel) });
            test.PostAction(bsonReader);

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
                dbCacheMock.Object,
                documentVersion,
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
                dbCacheMock.Object,
                documentVersion,
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
                        null,
                        new BsonDocument(new BsonElement("elem", BsonNull.Value)),
                        preAction: wr =>
                        {
                            wr.WriteStartDocument();
                            wr.WriteName("elem");
                        },
                        postAction: wr => wr.WriteEndDocument()),

                    // Complex model
                    new SerializationTestElement(
                        new FakeModel()
                        {
                            EnumerableProp = new[] { new FakeModel(), null },
                            Id = "idVal",
                            IntegerProp = 42,
                            ObjectProp = new FakeModel(),
                            StringProp = "yes"
                        },
                        new BsonDocument(new BsonElement[]
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
                        } as IEnumerable<BsonElement>)),

                    // True condition.
                    new SerializationTestElement(
                        new FakeModel()
                        {
                            Id = "idVal",
                        },
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", BsonNull.Value),
                            new BsonElement("IntegerProp", new BsonInt32(0)),
                            new BsonElement("ObjectProp", BsonNull.Value),
                            new BsonElement("StringProp", BsonNull.Value),
                            new BsonElement("ExtraElement", new BsonString("extraValue"))
                        } as IEnumerable<BsonElement>),
                        condition: _ => true),

                    // False condition.
                    new SerializationTestElement(
                        new FakeModel()
                        {
                            Id = "idVal",
                        },
                        new BsonDocument(new BsonElement[]
                        {
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", BsonNull.Value),
                            new BsonElement("IntegerProp", new BsonInt32(0)),
                            new BsonElement("ObjectProp", BsonNull.Value),
                            new BsonElement("StringProp", BsonNull.Value)
                        } as IEnumerable<BsonElement>),
                        condition: _ => false),
                    
                    // With a proxy class.
                    new SerializationTestElement(
                        new FakeModelProxy()
                        {
                            Id = "idVal",
                            IntegerProp = 42,
                            ObjectProp = new FakeModel(),
                            StringProp = "yes"
                        },
                        new BsonDocument(new BsonElement[]
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
                        } as IEnumerable<BsonElement>),
                        condition: _ => false)
                };

                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(SerializationTests))]
        public void Serialize(SerializationTestElement test)
        {
            // Setup
            var serializer = new ExtendedClassMapSerializer<FakeModel>(
                dbCacheMock.Object,
                documentVersion,
                serializerModifierAccessorMock.Object)
                .AddExtraElement(new BsonElement("ExtraElement", new BsonString("extraValue")), test.Condition);

            // Action
            test.PreAction(test.BsonWriter);
            serializer.Serialize(
                BsonSerializationContext.CreateRoot(test.BsonWriter),
                new BsonSerializationArgs { NominalType = typeof(FakeModel) },
                test.Model!);
            test.PostAction(test.BsonWriter);

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
                dbCacheMock.Object,
                documentVersion,
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
