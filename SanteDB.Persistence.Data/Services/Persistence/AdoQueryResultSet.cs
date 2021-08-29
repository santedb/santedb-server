using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents an Ado Persistence query set
    /// </summary>
    /// <remarks>This query set wraps the returns of Query methods and allows for
    /// delay loading of the resulting data from the underlying data provider</remarks>
    public class AdoQueryResultSet<TData> : IQueryResultSet<TData>, IDisposable
        where TData : IdentifiedData
    {

        // The data context
        private DataContext m_context;

        // The query provider
        private IAdoQueryProvider<TData> m_provider;

        // The result set that this wraps
        private IOrmResultSet m_resultSet;

        // Tracer 
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoQueryResultSet<TData>));

        /// <summary>
        /// Creates a new persistence collection
        /// </summary>
        internal AdoQueryResultSet(IAdoQueryProvider<TData> dataProvider)
        {
            this.m_provider = dataProvider;
            this.m_context = dataProvider.Provider.GetReadonlyConnection();
        }

        /// <summary>
        /// Create a wrapper persistence collection
        /// </summary>
        private AdoQueryResultSet(AdoQueryResultSet<TData> copyFrom, IOrmResultSet resultSet) : this(copyFrom.m_provider)
        {
            this.m_resultSet = resultSet;
            this.m_context = copyFrom.m_context;
        }


        /// <summary>
        /// Where clause for filtering on provider
        /// </summary>
        public IQueryResultSet<TData> Where(Expression<Func<TData, bool>> query)
        {
            if (this.m_resultSet != null)
            {
                throw new InvalidOperationException(ErrorMessages.ERR_ALREADY_HAS_WHERE);
            }
            else
            {
                return new AdoQueryResultSet<TData>(this, this.m_provider.ExecuteQueryOrm(this.m_context, query));
            }
        }

        /// <summary>
        /// Union the results in this query set with those in another
        /// </summary>
        public IQueryResultSet<TData> Union(Expression<Func<TData, bool>> query)
        {
            if (this.m_resultSet == null) // this is the first
            {
                return new AdoQueryResultSet<TData>(this, this.m_provider.ExecuteQueryOrm(this.m_context, query));
            }
            else
            {
                return new AdoQueryResultSet<TData>(this, this.m_resultSet.Union(this.m_provider.ExecuteQueryOrm(this.m_context, query)));
            }
        }

        /// <summary>
        /// Intersect with another dataset
        /// </summary>
        public IQueryResultSet<TData> Intersect(Expression<Func<TData, bool>> query)
        {
            if (this.m_resultSet == null) // this is the first
            {
                return new AdoQueryResultSet<TData>(this, this.m_provider.ExecuteQueryOrm(this.m_context, query));
            }
            else
            {
                return new AdoQueryResultSet<TData>(this, this.m_resultSet.Intersect(this.m_provider.ExecuteQueryOrm(this.m_context, query)));
            }
        }

        /// <summary>
        /// Skip the specified number of elements
        /// </summary>
        public IQueryResultSet<TData> Skip(int count)
        {
            if (this.m_resultSet == null) // this is the first
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }

            return new AdoQueryResultSet<TData>(this, this.m_resultSet.Skip(count));
        }

        /// <summary>
        /// Flattens the object into an enumerator
        /// </summary>
        public IEnumerator<TData> GetEnumerator()
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            try
            {
                this.m_context.Open();
                foreach (var result in this.m_resultSet)
                {
                    yield return this.m_provider.ToModelInstance(this.m_context, result);
                }
            }
            finally
            {
                this.m_context.Close();
#if DEBUG
                sw.Stop();
                this.m_tracer.TraceVerbose("Performance: GetEnumerator({0}) took {1}ms", this.m_resultSet, sw.ElapsedMilliseconds);
#endif
            }
        }

        /// <summary>
        /// Get enumerator for generic instances
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>
        /// Get the first instance of results
        /// </summary>
        /// <returns></returns>
        public TData First()
        {
            var retVal = this.FirstOrDefault();
            if (retVal == null)
            {
                throw new InvalidOperationException(ErrorMessages.ERR_SEQUENCE_NO_ELEMENTS);
            }
            return retVal;
        }

        /// <summary>
        /// Get the first or default of the result
        /// </summary>
        public TData FirstOrDefault()
        {
            if (this.m_resultSet == null)
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            try
            {
                this.m_context.Open();


                return this.m_provider.ToModelInstance(this.m_context, this.m_resultSet.FirstOrDefault());
            }
            finally
            {
                this.m_context.Close();
#if DEBUG
                sw.Stop();
                this.m_tracer.TraceVerbose("Performance: SingleOrDefault({0}) took {1}ms", this.m_resultSet, sw.ElapsedMilliseconds);
#endif
            }
        }

        /// <summary>
        /// Get only one object
        /// </summary>
        public TData Single()
        {
            var retVal = this.SingleOrDefault();
            if (retVal == null)
            {
                throw new InvalidOperationException(ErrorMessages.ERR_SEQUENCE_NO_ELEMENTS);
            }
            return retVal;
        }

        /// <summary>
        /// Get only one of the objects
        /// </summary>
        public TData SingleOrDefault()
        {
            if (this.m_resultSet == null)
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            try
            {
                this.m_context.Open();

                var resultCount = this.m_resultSet.Count();
                if (resultCount <= 1)
                {
                    return this.m_provider.ToModelInstance(this.m_context, this.m_resultSet.FirstOrDefault());
                }
                else
                {
                    throw new InvalidOperationException(ErrorMessages.ERR_SEQUENCE_MORE_THAN_ONE);
                }
            }
            finally
            {
                this.m_context.Close();
#if DEBUG
                sw.Stop();
                this.m_tracer.TraceVerbose("Performance: SingleOrDefault({0}) took {1}ms", this.m_resultSet, sw.ElapsedMilliseconds);
#endif
            }
        }

        /// <summary>
        /// Union this dataset with another
        /// </summary>
        public IQueryResultSet<TData> Union(IQueryResultSet<TData> other)
        {
            if (other is AdoQueryResultSet<TData> otherStrong)
            {
                if (this.m_resultSet != null && otherStrong.m_resultSet != null)
                {
                    return new AdoQueryResultSet<TData>(this, this.m_resultSet.Union(otherStrong.m_resultSet));
                }
                else
                {
                    throw new InvalidOperationException(ErrorMessages.ERR_INVALID_STATE);
                }
            }
            else
            {
                throw new ArgumentException(ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE);
            }
        }

        /// <summary>
        /// Takes the number of objects from the result set
        /// </summary>
        public IQueryResultSet<TData> Take(int count)
        {
            if (this.m_resultSet == null)
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }
            return new AdoQueryResultSet<TData>(this, this.m_resultSet.Take(count));
        }

        /// <summary>
        /// Append the order by statements onto the specifed result set
        /// </summary>
        public IQueryResultSet<TData> OrderBy(Expression<Func<TData, dynamic>> sortExpression)
        {
            if (this.m_resultSet == null) // this is the first
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }

            return new AdoQueryResultSet<TData>(this, this.m_resultSet.OrderBy(this.m_provider.MapSortExpression(sortExpression)));
        }

        /// <summary>
        /// Order result set by descending order
        /// </summary>
        public IQueryResultSet<TData> OrderByDescending(Expression<Func<TData, dynamic>> sortExpression)
        {
            if (this.m_resultSet == null) // this is the first
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }

            return new AdoQueryResultSet<TData>(this, this.m_resultSet.OrderByDescending(this.m_provider.MapSortExpression(sortExpression)));
        }

        /// <summary>
        /// Creates a query set or loads the specified query set 
        /// </summary>
        public IQueryResultSet<TData> AsStateful(Guid stateId)
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            try
            {
                if (this.m_provider.QueryPersistence == null)
                {
                    throw new InvalidOperationException(ErrorMessages.ERR_MISSING_SERVICE.Format(nameof(IQueryPersistenceService)));
                }

                // Is the query already registered? If so, load
                if (this.m_provider.QueryPersistence.IsRegistered(stateId))
                {
                    return new AdoStatefulResultSet<TData>(this.m_provider, stateId);
                }
                else
                {
                    this.m_context.Open();

                    Guid[] keySet = null;
                    if (typeof(IDbVersionedData).IsAssignableFrom(this.m_resultSet.ElementType))
                    {
                        keySet = this.m_resultSet.Select<Guid>(nameof(IDbVersionedData.Key)).Distinct().ToArray();
                    }
                    else
                    {
                         keySet = this.m_resultSet.Keys<Guid>().OfType<Guid>().ToArray();
                    }
                    this.m_provider.QueryPersistence.RegisterQuerySet(stateId, keySet, this.m_resultSet.Statement, keySet.Length);
                    return new AdoStatefulResultSet<TData>(this.m_provider, stateId);
                }
            }
            finally
            {
                this.m_context.Close();
#if DEBUG
                sw.Stop();
                this.m_tracer.TraceVerbose("Performance: AsStateful({0}) took {1}ms", stateId, sw.ElapsedMilliseconds);
#endif
            }
        }

        /// <summary>
        /// Return true if there are any matching reuslts
        /// </summary>
        public bool Any()
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            if (this.m_resultSet == null)
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }
            try
            {
                this.m_context.Open();
                return this.m_resultSet.Any();
            }
            finally {
                this.m_context.Close();
            }
        }

        /// <summary>
        /// Get the count of objects
        /// </summary>
        public int Count()
        {
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            if (this.m_resultSet == null)
            {
                this.m_resultSet = this.m_provider.ExecuteQueryOrm(this.m_context, o => true);
            }
            try
            {
                this.m_context.Open();
                return this.m_resultSet.Count();
            }
            finally
            {
                this.m_context.Close();
            }
        }

        /// <summary>
        /// Intersect with another result set
        /// </summary>
        public IQueryResultSet<TData> Intersect(IQueryResultSet<TData> other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Dispose of this object
        /// </summary>
        public void Dispose()
        {
            this.m_context.Dispose();
        }


        /// <summary>
        /// Non-generic version of where
        /// </summary>
        public IQueryResultSet Where(Expression query)
        {
            if (query is Expression<Func<TData, bool>> eq)
            {
                return this.Where(eq);
            }
            else
            {
                throw new ArgumentException(nameof(query), ErrorMessages.ERR_ARGUMENT_INCOMPATIBLE_TYPE.Format(typeof(Expression<Func<TData, bool>>)));
            }
        }

        /// <summary>
        /// Get the first object
        /// </summary>
        object IQueryResultSet.First() => this.First();

        /// <summary>
        /// Get the first or default (null) object
        /// </summary>
        object IQueryResultSet.FirstOrDefault() => this.FirstOrDefault();

        /// <summary>
        /// Get a single resut or throw
        /// </summary>
        object IQueryResultSet.Single() => this.Single();

        /// <summary>
        /// Get single result or default
        /// </summary>
        object IQueryResultSet.SingleOrDefault() => this.SingleOrDefault();

        /// <summary>
        /// Tag the specified <paramref name="count"/> objects
        /// </summary>
        IQueryResultSet IQueryResultSet.Take(int count) => this.Take(count);

        /// <summary>
        /// Skip the <paramref name="count"/> rsults
        /// </summary>
        IQueryResultSet IQueryResultSet.Skip(int count) => this.Skip(count);

        /// <summary>
        /// Represent as a stateful object
        /// </summary>
        IQueryResultSet IQueryResultSet.AsStateful(Guid stateId) => this.AsStateful(stateId);
    }
}
