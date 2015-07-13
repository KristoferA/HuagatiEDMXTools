using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
Copyright (C) 2010-2015, Huagati Systems Co., Ltd. - https://huagati.com

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

namespace HuagatiEDMXTools
{
    /// <summary>
    /// Exposes commonly used queries against the storage model. This class is accessible through the Queries property in the StorageModel class. 
    /// </summary>
    public class StorageModelQueries
    {
        private StorageModel _storageModel = null;
        internal StorageModelQueries(StorageModel storageModel)
        {
            _storageModel = storageModel;
        }

        /// <summary>
        /// Returns a list of entity sets with no mapping to the conceptual model.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <returns>A query (IQueryable) returning StoreEntitySet objects.</returns>
        public IQueryable<StoreEntitySet> GetUnmappedEntitySets(IEnumerable<string> exceptEntitySets)
        {
            return _storageModel.EntitySets
                .Where(es =>
                       (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(es.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    && !es.EntitySetMappings.Any()
                    && !es.AssociationSetMappings.Any())
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving entity type scalar members with no mapping to conceptual model scalar members
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptEntityTypes">Enumeration of names of entity types to exclude from the comparison</param>
        /// <param name="exceptMembers">Enumeration of names of members to exclude from the comparison</param>
        /// <returns>A query (IQueryable) of StoreMemberProperty objects.</returns>
        public IQueryable<StoreMemberProperty> GetUnmappedScalarMembers(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptEntityTypes, IEnumerable<string> exceptMembers)
        {
            return _storageModel.EntityTypes
                .Where(et =>
                        et.EntitySet != null
                    && (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(et.EntitySet.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    && (exceptEntityTypes == null || !exceptEntityTypes.Any(ign => ign.Equals(et.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    && !et.EntitySet.AssociationSetMappings.Any())
                .SelectMany(smp => smp.MemberProperties.Where(p => (exceptMembers == null || !exceptMembers.Any(ign => ign.Equals(smp.FullName, StringComparison.InvariantCultureIgnoreCase))) && !p.ModelMembers.Any() && !p.MappingConditions.Any()))
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving scalar members with attributes different than in one or more mapped conceptual model members.
        /// </summary>
        /// <returns>A query (IQueryable) of pairs of StoreMemberProperty objects and IEnumerables of the mapped ModelMemberProperties</returns>
        public IQueryable<Tuple<StoreMemberProperty, IEnumerable<ModelMemberProperty>>> GetChangedScalarMembers()
        {
            return GetChangedScalarMembers(null, null, null, true, true, true, true, true);
        }

        /// <summary>
        /// Query for retrieving scalar members with attributes different than in one or more mapped conceptual model members.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptEntityTypes">Enumeration of names of entity types to exclude from the comparison</param>
        /// <param name="exceptMembers">Enumeration of names of members to exclude from the comparison</param>
        /// <param name="compareCollation">Controls if collation is compared</param>
        /// <param name="compareDefaultValue">Controls if default value is compared</param>
        /// <param name="compareKeyMembership">Controls if entity key membership is compared</param>
        /// <param name="compareMemberType">Controls if member data type, nullability, length, scale/precision etc are compared</param>
        /// <param name="compareStoreGenerated">Controls if store generated pattern is compared</param>
        /// <returns>A query (IQueryable) of pairs of StoreMemberProperty objects and IEnumerables of the mapped ModelMemberProperties</returns>
        public IQueryable<Tuple<StoreMemberProperty, IEnumerable<ModelMemberProperty>>> GetChangedScalarMembers(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptEntityTypes, IEnumerable<string> exceptMembers, bool compareMemberType, bool compareKeyMembership, bool compareStoreGenerated, bool compareDefaultValue, bool compareCollation)
        {
            return _storageModel.EntityTypes
                .Where(et =>
                        et.EntitySet != null
                    && (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(et.EntitySet.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    && (exceptEntityTypes == null || !exceptEntityTypes.Any(ign => ign.Equals(et.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    )
                .SelectMany(
                smp => smp.MemberProperties.Where(
                    p => (exceptMembers == null || !exceptMembers.Any(im => im.Equals(smp.FullName, StringComparison.InvariantCultureIgnoreCase)))
                        && p.ModelMembers.Any(k =>
                            !k.IsComplexType
                            && ((compareKeyMembership == true && k.IsKey != p.IsKey)
                               || (compareMemberType == true && !k.CSDLType.Equals(p.CSDLType))
                               || (compareStoreGenerated == true && k.StoreGeneratedPattern != p.StoreGeneratedPattern && !k.EntityType.HasBaseType && !k.EntityType.HasSubTypes) //TODO: <= make more selective. requires changes in mapping lookup...
                               || (compareDefaultValue == true && k.DefaultValue != p.DefaultValue)
                               || (compareCollation == true && k.Collation != p.Collation))
                          )
                    )
                ).Select(smp => new Tuple<StoreMemberProperty, IEnumerable<ModelMemberProperty>>(smp, smp.ModelMembers.Where(mmp =>
                                  (compareKeyMembership == true && smp.IsKey != mmp.IsKey)
                               || (compareMemberType == true && !smp.CSDLType.Equals(mmp.CSDLType))
                               || (compareStoreGenerated == true && smp.StoreGeneratedPattern != mmp.StoreGeneratedPattern && !mmp.EntityType.HasBaseType && !mmp.EntityType.HasSubTypes) //TODO: <= make more selective. requires changes in mapping lookup...
                               || (compareDefaultValue == true && smp.DefaultValue != mmp.DefaultValue)
                               || (compareCollation == true && smp.Collation != mmp.Collation)
                               ))).AsQueryable();
        }

        /// <summary>
        /// Query for retrieving associations with no mapping to conceptual model associations.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptAssociations">Enumeration of names of model associations to exclude from comparison</param>
        /// <returns>A query (IQueryable) of StoreAssociationSet objects.</returns>
        public IQueryable<StoreAssociationSet> GetMissingAssociations(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptAssociations)
        {
            return _storageModel.AssociationSets
                .Where(
                    aset => aset.FromEntitySet != null
                        && aset.ToEntitySet != null
                        && aset.ModelAssociationSet == null
                        && (exceptEntitySets == null
                          || (
                                !exceptEntitySets.Any(ign => ign.Equals(aset.FromEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             && !exceptEntitySets.Any(ign => ign.Equals(aset.ToEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             )
                           )
                        && (exceptAssociations == null
                          || !exceptAssociations.Any(ign => ign.Equals(aset.FullName, StringComparison.InvariantCultureIgnoreCase))
                           )
                        && !aset.IsInheritanceConstraint()
                )
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving associations with changed keys.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptAssociations">Enumeration of names of model associations to exclude from comparison</param>
        /// <returns>A query (IQueryable) of StoreAssociationSet objects.</returns>
        public IQueryable<StoreAssociationSet> GetChangedAssociations(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptAssociations)
        {
            return _storageModel.AssociationSets
                .Where(
                    aset => aset.FromEntitySet != null
                        && aset.ToEntitySet != null
                        && aset.ModelAssociationSet != null
                        && !aset.FromEntitySet.IsJunctionCandidate
                        && (exceptEntitySets == null
                          || (
                                !exceptEntitySets.Any(ign => ign.Equals(aset.FromEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             && !exceptEntitySets.Any(ign => ign.Equals(aset.ToEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             )
                           )
                        && (exceptAssociations == null
                          || !exceptAssociations.Any(ign => ign.Equals(aset.FullName, StringComparison.InvariantCultureIgnoreCase))
                           )
                        && !aset.IsInheritanceConstraint()
                        && (aset.ModelAssociationSet.Keys.Count() > 0 && (aset.Keys.Count() != aset.ModelAssociationSet.Keys.Count()
                         || aset.ModelAssociationSet.Keys.SelectMany(k1 => k1.Item1.StoreMembers).Intersect(aset.Keys.Select(k1 => k1.Item1)).Count() != aset.Keys.Count()
                         || aset.ModelAssociationSet.Keys.SelectMany(k2 => k2.Item2.StoreMembers).Intersect(aset.Keys.Select(k2 => k2.Item2)).Count() != aset.Keys.Count()))
                )
                .AsQueryable();
        }
    }
}
