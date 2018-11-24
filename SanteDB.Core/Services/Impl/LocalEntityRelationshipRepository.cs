using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a local entity relationship repository
    /// </summary>
    public class LocalEntityRelationshipRepository : GenericLocalRepository<EntityRelationship>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryClinicalData;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadClinicalData;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteClinicalData;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteClinicalData;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteClinicalData;

        /// <summary>
        /// Insert the entity relationship
        /// </summary>
        public override EntityRelationship Insert(EntityRelationship data)
        {
            // force set the version sequence
            if (data.EffectiveVersionSequenceId == null)
                data.EffectiveVersionSequenceId = ApplicationContext.Current.GetService<IRepositoryService<Entity>>().Get(data.SourceEntityKey.Value, Guid.Empty)?.VersionSequence;

            return base.Insert(data);
        }

        /// <summary>
        /// Saves the specified data
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>TModel.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the persistence service is not found.</exception>
        public EntityRelationship Save(EntityRelationship data)
        {
            // force set the version sequence
            if (data.EffectiveVersionSequenceId == null)
                data.EffectiveVersionSequenceId = ApplicationContext.Current.GetService<IRepositoryService<Entity>>().Get(data.SourceEntityKey.Value, Guid.Empty)?.VersionSequence;

            return base.Save(data);
        }

    }
}
