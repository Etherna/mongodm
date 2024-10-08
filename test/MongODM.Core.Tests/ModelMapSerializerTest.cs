﻿// Copyright 2020-present Etherna SA
// This file is part of MongODM.
// 
// MongODM is free software: you can redistribute it and/or modify it under the terms of the
// GNU Lesser General Public License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// 
// MongODM is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along with MongODM.
// If not, see <https://www.gnu.org/licenses/>.

using Etherna.MongoDB.Bson;
using Etherna.MongoDB.Bson.IO;
using Etherna.MongoDB.Bson.Serialization;
using Etherna.MongODM.Core.Comparers;
using Etherna.MongODM.Core.Conventions;
using Etherna.MongODM.Core.Extensions;
using Etherna.MongODM.Core.Models;
using Etherna.MongODM.Core.Options;
using Etherna.MongODM.Core.Serialization.Mapping;
using Etherna.MongODM.Core.Serialization.Modifiers;
using Etherna.MongODM.Core.Serialization.Serializers;
using Etherna.MongODM.Core.Utility;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Etherna.MongODM.Core
{
    public class ModelMapSerializerTest
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
                Action<BsonWriter>? preAction = null,
                Action<BsonWriter>? postAction = null)
            {
                BsonWriter = new BsonDocumentWriter(SerializedDocument);
                ExpectedDocument = expectedDocument;
                Model = model;
                PreAction = preAction ?? (wr => { });
                PostAction = postAction ?? (wr => { });
            }

            public BsonWriter BsonWriter { get; }
            public BsonDocument ExpectedDocument { get; }
            public FakeModel? Model { get; }
            public Action<BsonWriter> PreAction { get; }
            public Action<BsonWriter> PostAction { get; }
            public BsonDocument SerializedDocument { get; } = new BsonDocument();
        }

        // Fields.
        private readonly Mock<IDbCache> dbCacheMock = new();
        private readonly Mock<IDbContext> dbContextMock = new();
        private readonly Mock<IDiscriminatorRegistry> discriminatorRegistryMock = new();
        private readonly Mock<IModelMap> modelMapMock = new();
        private readonly ModelMapVersionOptions modelMapVersionOptions = new();
        private readonly Mock<IMapRegistry> mapRegistryMock = new();
        private readonly Mock<ISerializerModifierAccessor> serializerModifierAccessorMock = new();

        // Constructor.
        public ModelMapSerializerTest()
        {
            discriminatorRegistryMock.Setup(r => r.LookupDiscriminatorConvention(It.IsAny<Type>()))
                .Returns(() => new HierarchicalProxyTolerantDiscriminatorConvention(dbContextMock.Object, "_t"));

            dbCacheMock.Setup(c => c.LoadedModels.ContainsKey(It.IsAny<object>()))
                .Returns(() => false);

            dbContextMock.Setup(c => c.DbCache)
                .Returns(() => dbCacheMock.Object);
            dbContextMock.Setup(c => c.DiscriminatorRegistry)
                .Returns(() => discriminatorRegistryMock.Object);
            dbContextMock.Setup(c => c.ProxyGenerator.IsProxyType(It.IsAny<Type>()))
                .Returns(true);
            dbContextMock.Setup(c => c.Options.ModelMapVersion)
                .Returns(() => modelMapVersionOptions);
            dbContextMock.Setup(c => c.MapRegistry)
                .Returns(() => mapRegistryMock.Object);
            dbContextMock.Setup(c => c.SerializerModifierAccessor)
                .Returns(() => serializerModifierAccessorMock.Object);

            mapRegistryMock.Setup(sr => sr.GetModelMap(typeof(FakeModel)))
                .Returns(() => modelMapMock.Object);
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
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            var serializer = new ModelMapSerializer<FakeModel>(dbContextMock.Object);

            modelMapMock.Setup(s => s.ActiveSchema.BsonClassMap)
                .Returns(classMap);
            modelMapMock.Setup(s => s.ActiveSchema.Serializer)
                .Returns(classMap.ToSerializer());
            modelMapMock.Setup(s => s.ActiveSchema.FixDeserializedModelAsync(It.IsAny<object>()))
                .Returns<object>(m => Task.FromResult(m));

            // Action
            test.PreAction(bsonReader);
            var result = serializer.Deserialize(
                BsonDeserializationContext.CreateRoot(bsonReader),
                new BsonDeserializationArgs { NominalType = typeof(FakeModel) });
            test.PostAction(bsonReader);

            // Assert
            Assert.Equal(test.ExpectedModel, result, new FakeModelComparer());
        }

        [Fact]
        public void GetDocumentId()
        {
            // Setup
            var model = new FakeModel { Id = "idVal" };
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            var bsonClassMapSerializer = new BsonClassMapSerializer<FakeModel>(classMap);
            var serializer = new ModelMapSerializer<FakeModel>(dbContextMock.Object);

            modelMapMock.Setup(s => s.ActiveSchema.BsonClassMap)
                .Returns(classMap);

            // Action
            var result = serializer.GetDocumentId(
                model,
                out object idResult,
                out Type idNominalTypeResult,
                out IIdGenerator idGeneratorResult);

            // Assert
            var resultExpected = bsonClassMapSerializer.GetDocumentId(
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
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            var bsonClassMapSerializer = new BsonClassMapSerializer<FakeModel>(classMap);
            var serializer = new ModelMapSerializer<FakeModel>(dbContextMock.Object);

            modelMapMock.Setup(s => s.ActiveSchema.BsonClassMap)
                .Returns(classMap);

            // Action
            var result = serializer.TryGetMemberSerializationInfo(memberName, out BsonSerializationInfo serializationInfo);

            // Assert
            var expectedResult = bsonClassMapSerializer.TryGetMemberSerializationInfo(memberName,
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
                            new BsonElement("_m", new BsonString("mapId")),
                            new BsonElement("_id", new BsonString("idVal")),
                            new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                            new BsonElement("EnumerableProp", new BsonArray(new BsonValue[]
                            {
                                new BsonDocument(new BsonElement[]
                                {
                                    /*commented because serializer is not registered*/
                                    //new BsonElement("_m", new BsonString("mapId")),
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
                                /*commented because serializer is not registered*/
                                //new BsonElement("_m", new BsonString("mapId")),
                                new BsonElement("_id", BsonNull.Value),
                                new BsonElement("CreationDateTime", new BsonDateTime(new DateTime())),
                                new BsonElement("EnumerableProp", BsonNull.Value),
                                new BsonElement("IntegerProp", new BsonInt32(0)),
                                new BsonElement("ObjectProp", BsonNull.Value),
                                new BsonElement("StringProp", BsonNull.Value)
                            } as IEnumerable<BsonElement>)),
                            new BsonElement("StringProp", new BsonString("yes")),
                        } as IEnumerable<BsonElement>)),
                };

                return tests.Select(t => new object[] { t });
            }
        }

        [Theory, MemberData(nameof(SerializationTests))]
        public void Serialize(SerializationTestElement test)
        {
            // Setup
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            var serializer = new ModelMapSerializer<FakeModel>(dbContextMock.Object);

            modelMapMock.Setup(s => s.ActiveSchema.BsonClassMap)
                .Returns(classMap);
            modelMapMock.Setup(s => s.ActiveSchema.Id)
                .Returns("mapId");

            mapRegistryMock.Setup(sr => sr.GetActiveModelMapIdBsonElement(typeof(FakeModel)))
                .Returns(new BsonElement("_m", new BsonString("mapId")));

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
            var classMap = new BsonClassMap<FakeModel>(cm => cm.AutoMap());
            classMap.Freeze();
            var bsonClassMapSerializer = new BsonClassMapSerializer<FakeModel>(classMap);
            var serializer = new ModelMapSerializer<FakeModel>(dbContextMock.Object);

            modelMapMock.Setup(s => s.ActiveSchema.BsonClassMap)
                .Returns(classMap);

            // Action
            serializer.SetDocumentId(model, id);

            // Assert
            var expectedModel = new FakeModel();
            bsonClassMapSerializer.SetDocumentId(expectedModel, id);
            Assert.Equal(expectedModel, model, new FakeModelComparer());
        }
    }
}
