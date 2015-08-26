using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Google.Apis.Datastore.v1beta2.Data;

namespace GoogleAppEngine.Datastore.LINQ
{
    internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable
    {
        ProjectionEnumerator _enumerator;
        
        internal ProjectionReader(IEnumerable<Entity> entity, Func<ProjectionRow, T> projector)
        {
            this._enumerator = new ProjectionEnumerator(entity, projector);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this._enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        class ProjectionEnumerator : ProjectionRow, IEnumerator<T>, IEnumerator, IDisposable
        {
            T _current;
            Func<ProjectionRow, T> _projector;
            Entity _currentEntity;
            IEnumerator<Entity> _entityPropertyEnumerator;

            internal ProjectionEnumerator(IEnumerable<Entity> entities, Func<ProjectionRow, T> projector)
            {
                this._projector = projector;
                this._entityPropertyEnumerator = entities.GetEnumerator();
            }

            private DateTime? deserializeDateTimeProjection(long? datetime)
            {
                if (!datetime.HasValue)
                    return null;

                return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
                    .Add(new TimeSpan(datetime.Value * (TimeSpan.TicksPerMillisecond / 1000)));
            }

            // TODO need to refactor this and the one in serializer - make common base
            public override object GetValue(string columnName, TypeCode typecode)
            {
                // Check if it's a projection aiming for a key
                if (columnName.ToLower() == "id" ||
                    typeof(T).GetProperty(columnName)?.CustomAttributes?.Any(x => x.AttributeType == typeof (DatastoreKeyAttribute)) == true)
                    return _currentEntity.Key.Path[0].Name;

                var property = _currentEntity.Properties.First(x => x.Key == columnName);
                var propValue = property.Value;

                switch (typecode)
                {
                    case TypeCode.Boolean:
                        return propValue.BooleanValue ?? default(bool);
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                        return propValue.IntegerValue ?? default(int);
                    case TypeCode.DateTime:
                        // Google's C# client library has an issue -- it does not deserialize projections correctly, so we have to fix it
                        return propValue.DateTimeValue ?? deserializeDateTimeProjection(propValue.IntegerValue) ?? default(DateTime);
                    case TypeCode.String:
                        return propValue.StringValue;
                    case TypeCode.Double:
                        return propValue.DoubleValue ?? default(double);
                    case TypeCode.Decimal:
                        return string.IsNullOrWhiteSpace(propValue.StringValue) ? default(decimal) : Convert.ToDecimal(propValue.StringValue);
                    default:
                        throw new NotSupportedException($"The type of `{property.Key}` is not supported.");
                }
            }

            public T Current
            {
                get { return this._current; }
            }

            object IEnumerator.Current
            {
                get { return this._current; }
            }

            public bool MoveNext()
            {
                // Only project so long as the returned entity contains the property --
                // not whether or not the projection contains the field
                if (_entityPropertyEnumerator.MoveNext())
                {
                    this._currentEntity = _entityPropertyEnumerator.Current;
                    this._current = this._projector(this);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
