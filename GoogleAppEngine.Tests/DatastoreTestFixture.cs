using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleAppEngine.Tests
{
    public class DatastoreTestFixture
    {
        private readonly DatastoreTestTranslator<TestModel> _datastoreTestTranslator;

        protected enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        protected class TestClass { }

        protected class TestModel
        {
            public bool BoolValue { get; set; }
            public bool BoolValue2 { get; set; }
            public string StringValue { get; set; }
            public int IntValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public DateTime DatetimeValue { get; set; }
            public TestEnum EnumValue { get; set; }
            public List<TestClass> ListValue { get; set; }
            public byte[] ByteArrayValue { get; set; }
            public Dictionary<string, int> DictionaryValue { get; set; }
            public short ShortValue { get; set; }
            public long LongValue { get; set; }
        }

        public DatastoreTestFixture()
        {
            _datastoreTestTranslator = new DatastoreTestTranslator<TestModel>();
        }

        protected IQueryable<TestModel> GetQueryable()
        {
            return new DatastoreTestQueryable<TestModel>(_datastoreTestTranslator);
        }

        protected string GetQueryText()
        {
            return _datastoreTestTranslator.GetQueryText();
        }
    }
}
