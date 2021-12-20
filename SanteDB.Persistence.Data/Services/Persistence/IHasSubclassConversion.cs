﻿using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service that has subclass conversion
    /// </summary>
    /// <remarks>
    /// When calling a get on a class such as Entity or Material, the classification code dictates the actual class
    /// type which is stored. This interface is implemented by any derived interface so that the conversion process
    /// can reach out to the higher level handler and "convert" it to a desired type.
    /// </remarks>
    internal interface IHasSubclassConversion : IAdoPersistenceProvider
    {

        /// <summary>
        /// Convert <paramref name="existingModel"/> to the apropriate target model
        /// </summary>
        /// <param name="context">The context on which this is being converted</param>
        /// <param name="existingModel">The existing model object</param>
        /// <param name="dbModel">The existing database model data loaded from the store</param>
        /// <param name="referenceObjects">Additional reference objects which have also been loaded from the database</param>
        /// <returns>The converted object</returns>
        object Convert(DataContext context, object existingModel, object dbModel, params object[] referenceObjects);
    }
}
