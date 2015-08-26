using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Datastore.v1beta2.Data;
using Google.Apis.Services;
using GoogleAppEngine.Datastore;
using GoogleAppEngine.Datastore.Indexing;
using GoogleAppEngine.Datastore.LINQ;
using GoogleAppEngine.Datastore.Serialization;
using GoogleAppEngine.Shared;
using LightMock;
using Xunit;

namespace GoogleAppEngine.Tests
{
    public class DatastoreTest : DatastoreTestFixture
    {
        [Fact]
        public void Datastore_serializer_must_parameterize_properties()
        {
            var entity = new TestModel();
            var serialized = new DatastoreSerializer<TestModel>().SerializeEntity(entity);
            Assert.Equal(serialized.Properties.Count, typeof(TestModel).GetProperties().Count(x => x.GetValue(entity, null) != null));
        }

        [Fact]
        public void Datastore_service_find_returns_queryable_interface()
        {
            var testQueryable = (IOrderedQueryable<TestModel>)GetQueryable();

            var mockContext = new MockContext<IDatastoreService>();
            mockContext.Arrange(f => f.Find<TestModel>()).Returns(testQueryable);

            var datastoreMock = new DatastoreServiceMock(mockContext);
            var result = datastoreMock.Find<TestModel>();

            mockContext.Assert(f => f.Find<TestModel>(), Invoked.Once);
            Assert.Equal(testQueryable, result);
        }

        [Fact]
        public void Datastore_service_delete_accepts_entity_object()
        {
            var mockContext = new MockContext<IDatastoreService>();
            var datastoreMock = new DatastoreServiceMock(mockContext);
            datastoreMock.Delete(new TestModel());
            mockContext.Assert(f => f.Delete(The<TestModel>.IsAnyValue));
        }

        [Fact]
        public void Datastore_service_delete_accepts_predicate()
        {
            var mockContext = new MockContext<IDatastoreService>();
            var datastoreMock = new DatastoreServiceMock(mockContext);
            datastoreMock.Delete<TestModel>(x => true);
            mockContext.Assert(f => f.Delete(The<Func<TestModel, bool>>.IsAnyValue));
        }

        [Fact]
        public void Datastore_service_delete_accepts_enumerable()
        {
            var mockContext = new MockContext<IDatastoreService>();
            var datastoreMock = new DatastoreServiceMock(mockContext);
            datastoreMock.DeleteRange(new List<TestModel>());
            mockContext.Assert(f => f.DeleteRange(The<IEnumerable<TestModel>>.IsAnyValue));
        }

        [Fact]
        public void Datastore_service_upsert_accepts_entity()
        {
            var mockContext = new MockContext<IDatastoreService>();
            var datastoreMock = new DatastoreServiceMock(mockContext);
            datastoreMock.Upsert(new TestModel());
            mockContext.Assert(f => f.Upsert(The<TestModel>.IsAnyValue));
        }

        [Fact]
        public void Datastore_service_upsert_accepts_enumerable()
        {
            var mockContext = new MockContext<IDatastoreService>();
            var datastoreMock = new DatastoreServiceMock(mockContext);
            datastoreMock.UpsertRange(new List<TestModel>());
            mockContext.Assert(f => f.UpsertRange(The<IEnumerable<TestModel>>.IsAnyValue));
        }

        [Fact]
        public void Datastore_service_gql_accepts_query_object()
        {
            var runQueryResponse = new RunQueryResponse();
            var mockContext = new MockContext<IDatastoreService>();
            mockContext.Arrange(f => f.Gql(The<GqlQuery>.IsAnyValue)).Returns(runQueryResponse);
            var datastoreMock = new DatastoreServiceMock(mockContext);

            var response = datastoreMock.Gql(new GqlQuery());

            mockContext.Assert(f => f.Gql(The<GqlQuery>.IsAnyValue));
            Assert.Equal(runQueryResponse, response);
        }

        [Fact]
        public void Datastore_queryable_where_constant_false_should_throw()
        {
            Assert.Throws<InvalidOperationException>(() => GetQueryable().Where(x => false).Translate());
        }
        
        [Fact]
        public void Datastore_queryable_where_access_nonprimitives_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => GetQueryable().Where(x => x.ListValue.Any()).Translate());
            Assert.Throws<NotSupportedException>(() => GetQueryable().Where(x => x.DictionaryValue.ContainsValue(0)).Translate());
        }

        [Fact]
        public void Datastore_queryable_where_string_contains_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => GetQueryable().Where(x => x.StringValue.Contains("t")).Translate());
        }

        [Fact]
        public void Datastore_queryable_where_constant_true_selects_all()
        {
            GetQueryable().Where(x => true).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_boolean()
        {
            GetQueryable().Where(x => x.BoolValue).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_unary_not_for_boolean()
        {
            GetQueryable().Where(x => !x.BoolValue).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_binary_boolean_true_comparison()
        {
            GetQueryable().Where(x => x.BoolValue == true).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_binary_boolean_false_comparison()
        {
            GetQueryable().Where(x => x.BoolValue == false).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_boolean_false_nested_binary_comparison()
        {
            GetQueryable().Where(x => x.BoolValue == false && x.IntValue >= 4).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND IntValue >= @4", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_boolean_true_nested_binary_comparison()
        {
            GetQueryable().Where(x => x.BoolValue == true && x.IntValue >= 4).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True AND IntValue >= @4", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_boolean_true_binary_comparison()
        {
            GetQueryable().Where(x => x.BoolValue && x.IntValue >= 4).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True AND IntValue >= @4", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_boolean_false_binary_comparison()
        {
            GetQueryable().Where(x => !x.BoolValue && x.IntValue >= 4).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND IntValue >= @4", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_false()
        {
            GetQueryable().Where(x => !x.BoolValue && !x.BoolValue2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND BoolValue2 = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_true()
        {
            GetQueryable().Where(x => x.BoolValue && x.BoolValue2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True AND BoolValue2 = @True", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_mixed()
        {
            GetQueryable().Where(x => x.BoolValue && !x.BoolValue2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True AND BoolValue2 = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_binary_true()
        {
            GetQueryable().Where(x => x.BoolValue == true && x.BoolValue2 == true).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @True AND BoolValue2 = @True", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_binary_false()
        {
            GetQueryable().Where(x => x.BoolValue == false && x.BoolValue2 == false).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND BoolValue2 = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_binary_mixed()
        {
            GetQueryable().Where(x => x.BoolValue == false && x.BoolValue2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND BoolValue2 = @True", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_boolean_binary_mixed_two()
        {
            GetQueryable().Where(x => x.BoolValue == false && !x.BoolValue2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND BoolValue2 = @False", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_double_greater_than()
        {
            GetQueryable().Where(x => x.DoubleValue > 14.5).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DoubleValue > @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_double_less_than()
        {
            GetQueryable().Where(x => x.DoubleValue < 14.5).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DoubleValue < @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_double_equal_to()
        {
            GetQueryable().Where(x => x.DoubleValue == 14.5).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DoubleValue = @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_double_less_than_or_equal_to()
        {
            GetQueryable().Where(x => x.DoubleValue <= 14.5).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DoubleValue <= @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_double_greater_than_or_equal_to()
        {
            GetQueryable().Where(x => x.DoubleValue >= 14.5).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DoubleValue >= @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_decimal_equalto()
        {
            GetQueryable().Where(x => x.DecimalValue == 14.5m).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DecimalValue = @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_decimal_less_than()
        {
            GetQueryable().Where(x => x.DecimalValue < 14.5m).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DecimalValue < @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_decimal_greater_than()
        {
            GetQueryable().Where(x => x.DecimalValue > 14.5m).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DecimalValue > @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_decimal_greater_than_or_equal_to()
        {
            GetQueryable().Where(x => x.DecimalValue >= 14.5m).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DecimalValue >= @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_decimal_less_than_or_equal_to()
        {
            GetQueryable().Where(x => x.DecimalValue <= 14.5m).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DecimalValue <= @14.5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_multiple_comparisons()
        {
            GetQueryable().Where(x => x.StringValue == "StringValue" && x.IntValue == 4).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @StringValue AND IntValue = @4", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_datetime()
        {
            var datetime = new DateTime(1994, 10, 10, 0, 0, 0, DateTimeKind.Utc);
            GetQueryable().Where(x => x.DatetimeValue == datetime).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE DatetimeValue = @1994-10-10T00:00:00Z", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_enum()
        {
            GetQueryable().Where(x => x.EnumValue == TestEnum.Value2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE EnumValue = @Value2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_where_should_evaluate_bool_and_other()
        {
            GetQueryable().Where(x => x.ShortValue == 100 && !x.BoolValue).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE ShortValue = @100 AND BoolValue = @False", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_scalar()
        {
            GetQueryable().Select(x => x.LongValue).Translate();
            Assert.Equal("SELECT LongValue FROM TestModel", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_anonymous_one()
        {
            GetQueryable().Select(x => new { x.LongValue }).Translate();
            Assert.Equal("SELECT LongValue FROM TestModel", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_anonymous_two()
        {
            GetQueryable().Select(x => new { x.LongValue, x.StringValue }).Translate();
            Assert.Equal("SELECT LongValue, StringValue FROM TestModel", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_anonymous_three()
        {
            GetQueryable().Select(x => new { x.LongValue, x.StringValue, x.ByteArrayValue }).Translate();
            Assert.Equal("SELECT LongValue, StringValue, ByteArrayValue FROM TestModel", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_self_as_all()
        {
            GetQueryable().Select(x => x).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }
        
        [Fact]
        public void Datastore_queryable_select_should_project_anonymous_nested()
        {
            GetQueryable().Select(x => new { x.StringValue, IntValues = new { x.ShortValue, x.LongValue, x.IntValue } }).Translate();
            Assert.Equal("SELECT StringValue, ShortValue, LongValue, IntValue FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_select_should_project_empty_anonymous_as_all()
        {
            GetQueryable().Select(x => new {}).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_select_should_project_literal_anonymous_as_all()
        {
            GetQueryable().Select(x => new { Z = new byte[0] }).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_select_should_project_literal_constant_as_all()
        {
            GetQueryable().Select(x => 0).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_select_should_project_null_object_as_all()
        {
            GetQueryable().Select(x => (string)null).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_select_should_project_local_vars_as_all()
        {
            var testObject = new TestClass();
            const int intval = 0;
            GetQueryable().Select(x => new { z = testObject, v = intval, y = default(long) }).Translate();
            Assert.Equal("SELECT * FROM TestModel", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_take()
        {
            GetQueryable().Take(5).Translate();
            Assert.Equal("SELECT * FROM TestModel LIMIT @5", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_take_and_skip()
        {
            GetQueryable().Take(5).Skip(2).Translate();
            Assert.Equal("SELECT * FROM TestModel LIMIT @5 OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_take_and_skip_with_where()
        {
            GetQueryable().Where(x => x.StringValue == "test").Take(5).Skip(2).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @test LIMIT @5 OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_take_and_skip_with_where_and_projection()
        {
            GetQueryable().Where(x => x.StringValue == "test").Take(5).Skip(2).Select(x => x.ShortValue).Translate();
            Assert.Equal("SELECT ShortValue FROM TestModel WHERE StringValue = @test LIMIT @5 OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_reordered_take_and_skip_with_where_and_projection()
        {
            GetQueryable().Take(5).Skip(2).Where(x => x.StringValue == "test").Select(x => x.ShortValue).Translate();
            Assert.Equal("SELECT ShortValue FROM TestModel WHERE StringValue = @test LIMIT @5 OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_orderby()
        {
            GetQueryable().OrderBy(x => x.IntValue).Translate();
            Assert.Equal("SELECT * FROM TestModel ORDER BY IntValue ASC", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_orderbydescending()
        {
            GetQueryable().OrderByDescending(x => x.IntValue).Translate();
            Assert.Equal("SELECT * FROM TestModel ORDER BY IntValue DESC", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_orderby_with_where_take_skip_and_select()
        {
            GetQueryable().Where(x => x.BoolValue && x.IntValue <= 10).Take(5).Skip(2).OrderBy(x => x.IntValue).Select(x => x.DatetimeValue).Translate();
            Assert.Equal("SELECT DatetimeValue FROM TestModel WHERE BoolValue = @True AND IntValue <= @10 ORDER BY IntValue ASC LIMIT @5 OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_first_with_take_one()
        {
            GetQueryable().First();
            Assert.Equal("SELECT * FROM TestModel LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_firstordefault_with_take_one()
        {
            GetQueryable().FirstOrDefault();
            Assert.Equal("SELECT * FROM TestModel LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_single_with_take_two()
        {
            GetQueryable().Single();
            Assert.Equal("SELECT * FROM TestModel LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_singleordefault_with_take_two()
        {
            GetQueryable().SingleOrDefault();
            Assert.Equal("SELECT * FROM TestModel LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_throw_when_first_and_take()
        {
            Assert.Throws<InvalidOperationException>(() => GetQueryable().Take(2).First());
        }

        [Fact]
        public void Datastore_queryable_should_ignore_redundant_takes()
        {
            GetQueryable().Take(2).Take(1).Translate();
            Assert.Equal("SELECT * FROM TestModel LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_ignore_redundant_skips()
        {
            GetQueryable().Skip(2).Skip(1).Translate();
            Assert.Equal("SELECT * FROM TestModel OFFSET @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_first_with_predicate()
        {
            GetQueryable().First(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_firstordefault_with_predicate()
        {
            GetQueryable().FirstOrDefault(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_single_with_predicate()
        {
            GetQueryable().Single(x => x.BoolValue == false && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND LongValue = @0 LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_singleordefault_with_predicate()
        {
            GetQueryable().SingleOrDefault(x => x.BoolValue == false && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND LongValue = @0 LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_firstordefault_with_where_and_predicate()
        {
            GetQueryable().Where(x => x.StringValue == "t").FirstOrDefault(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @t AND BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_singleordefault_with_where_and_predicate()
        {
            GetQueryable().Where(x => x.StringValue == "t").SingleOrDefault(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @t AND BoolValue = @False AND LongValue = @0 LIMIT @2", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_any_without_predicate()
        {
            GetQueryable().Any();
            Assert.Equal("SELECT * FROM TestModel LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_evaluate_any_with_predicate()
        {
            GetQueryable().Any(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_merge_where_and_any_with_predicate()
        {
            GetQueryable().Where(x => x.StringValue == "t").Any(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @t AND BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_merge_where_and_first_with_predicate()
        {
            GetQueryable().Where(x => x.StringValue == "t").First(x => !x.BoolValue && x.LongValue == 0);
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @t AND BoolValue = @False AND LongValue = @0 LIMIT @1", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_should_merge_multiple_where()
        {
            GetQueryable().Where(x => x.StringValue == "t").Where(x => !x.BoolValue && x.LongValue == 0).Translate();
            Assert.Equal("SELECT * FROM TestModel WHERE StringValue = @t AND BoolValue = @False AND LongValue = @0", GetQueryText());
        }

        [Fact]
        public void Datastore_queryable_or_operator_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => GetQueryable().Where(x => x.BoolValue || x.IntValue == 0).Translate());
        }

        [Fact]
        public void Datastore_queryable_not_equal_to_operator_should_throw()
        {
            Assert.Throws<NotSupportedException>(() => GetQueryable().Where(x => x.IntValue != 0).Translate());
        }
    }
}
