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
    /// Class containing common / useful queries against the conceptual model. Access through the ConceptualModel class' Queries property.
    /// </summary>
    public class ConceptualModelQueries
    {
        private ConceptualModel _conceptualModel = null;
        internal ConceptualModelQueries(ConceptualModel conceptualModel)
        {
            _conceptualModel = conceptualModel;
        }

        /// <summary>
        /// Query for retrieving common properties; scalar mebmers that exist in more than one entity type with the same name, type, etc.
        /// </summary>
        /// <returns>A query (IQueryable) of CommonModelProperty objects.</returns>
        public IQueryable<CommonModelProperty> GetCommonProperties()
        {
            return GetCommonProperties(false, false, false);
        }

        /// <summary>
        /// Query for retrieving common properties; properties that exist in more than one entity type with the same name, type, etc.
        /// </summary>
        /// <param name="includePKMembers">Controls whether primary key members should be considered/included.</param>
        /// <param name="includeComplexTypeRefs">Controls whether complex type reference members should be considered/included.</param>
        /// <param name="includeFKMembers">Controls whether members that participate in association sets as keys should be considered/included</param>
        /// <returns>A query (IQueryable) of CommonModelProperty objects.</returns>
        public IQueryable<CommonModelProperty> GetCommonProperties(bool includePKMembers, bool includeComplexTypeRefs, bool includeFKMembers)
        {
            IQueryable<ModelMemberProperty> allMemberProperties =
                _conceptualModel.EntityTypes.SelectMany(mp => mp.MemberProperties).AsQueryable();

            IQueryable<CommonModelProperty> commonMemberProperties =
                from mp in allMemberProperties
                where (includePKMembers == true || mp.IsKey == false)
                   && (includeComplexTypeRefs == true || !_conceptualModel.ComplexTypes.Any(ct => ct.FullName == mp.TypeName || ct.AliasName == mp.TypeName))
                   && (includeFKMembers == true || (!mp.EntityType.AssociationsFrom.Any(af => af.Keys.Any(k => k.Item1 == mp) && !mp.EntityType.AssociationsTo.Any(at => af.Keys.Any(k => k.Item2 == mp)))))
                group mp by new CommonModelProperty { Name = mp.Name, TypeName = mp.TypeName, Nullable = mp.Nullable, MaxLength = mp.MaxLength, Precision = mp.Precision, Scale = mp.Scale, TypeDescription = mp.TypeDescription } into mpg
                select new CommonModelProperty(mpg.Key) { EntityTypes = mpg.Select(m => m.EntityType).Distinct().ToList() };

            commonMemberProperties = commonMemberProperties.Where(mp => mp.EntityTypes.Count > 1).OrderBy(o1 => o1.Name).OrderByDescending(o2 => o2.EntityTypes.Count);

            return commonMemberProperties;
        }

        /// <summary>
        /// Query for retrieving common properties; properties that exist in more than one entity type with the same name, type, etc.
        /// </summary>
        /// <param name="filterEntityTypes">A filter of entity types. If set, only members that exist in all of the entity types passed in will be considered by the query.</param>
        /// <param name="includePKMembers">Controls whether primary key members should be considered/included.</param>
        /// <param name="includeComplexTypeRefs">Controls whether complex type reference members should be considered/included.</param>
        /// <param name="includeFKMembers">Controls whether members that participate in association sets as keys should be considered/included</param>
        /// <returns>A query (IQueryable) of CommonModelProperty objects.</returns>
        public IQueryable<CommonModelProperty> GetCommonProperties(IEnumerable<ModelEntityType> filterEntityTypes, bool includePKMembers, bool includeComplexTypeRefs, bool includeFKMembers)
        {
            IQueryable<CommonModelProperty> commonMemberProperties = GetCommonProperties(includePKMembers, includeComplexTypeRefs, includeFKMembers);

            if (filterEntityTypes != null)
            {
                commonMemberProperties = commonMemberProperties.Where(mp => mp.EntityTypes.Intersect(filterEntityTypes).Any());
            }

            return commonMemberProperties;
        }

        /// <summary>
        /// Query for determining what entity types contain all of a set of model properties.
        /// </summary>
        /// <param name="memberPropertyDescriptions">An IEnumerable of the model properties that need to be present in all entity types returned by the query.</param>
        /// <returns>A query (IQueryable) of ModelEntityType objects.</returns>
        public IQueryable<ModelEntityType> GetTypesWithMembers(IEnumerable<CommonModelProperty> memberPropertyDescriptions)
        {
            IQueryable<ModelEntityType> modelEntityTypes = _conceptualModel.EntityTypes.AsQueryable();

            foreach (CommonModelProperty memberPropertyDescription in memberPropertyDescriptions)
            {
                CommonModelProperty mpd = memberPropertyDescription;
                modelEntityTypes = modelEntityTypes.Where(et => mpd.EntityTypes.Contains(et));
            }

            modelEntityTypes = modelEntityTypes.OrderBy(et => et.Name);

            return modelEntityTypes;
        }

        /// <summary>
        /// Query for retrieving entity sets with no mapping to storage entitysets
        /// </summary>
        /// <returns>A query (IQueryable) of ModelEntitySet objects.</returns>
        public IQueryable<ModelEntitySet> GetUnmappedEntitySets()
        {
            return _conceptualModel.EntitySets.AsQueryable().Where(es => es.EntitySetMapping == null);
        }

        /// <summary>
        /// Query for retrieving entity type scalar members with no mapping to storage model scalar members
        /// </summary>
        /// <returns>A query (IQueryable) of ModelMemberProperty objects.</returns>
        public IQueryable<ModelMemberProperty> GetUnmappedScalarMembers()
        {
            return GetUnmappedScalarMembers(null, null, null);
        }

        /// <summary>
        /// Query for retrieving entity type scalar members with no mapping to storage model scalar members
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptEntityTypes">Enumeration of names of entity types to exclude from the comparison</param>
        /// <param name="exceptMembers">Enumeration of names of members to exclude from the comparison</param>
        /// <returns>A query (IQueryable) of ModelMemberProperty objects.</returns>
        public IQueryable<ModelMemberProperty> GetUnmappedScalarMembers(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptEntityTypes, IEnumerable<string> exceptMembers)
        {
            return _conceptualModel.EntityTypes
                .Where(et => et.EntitySet != null
                          && (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(et.EntitySet.FullName, StringComparison.InvariantCultureIgnoreCase)))
                          && (exceptEntityTypes == null || !exceptEntityTypes.Any(ign => ign.Equals(et.FullName, StringComparison.InvariantCultureIgnoreCase))))
                .SelectMany(
                    mmp => mmp.MemberProperties
                        .Where(
                            p => (exceptMembers == null || !exceptMembers.Any(im => im.Equals(mmp.FullName, StringComparison.InvariantCultureIgnoreCase)))
                                && !p.StoreMembers.Any()
                                && !p.IsComplexType))
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving scalar members with attribute differences between the conceptual model and storage model.
        /// </summary>
        /// <returns>A query returning tuples of ModelMemberProperty objects paired with IEnumerable of the mapped store member properties with attribute mismatches.</returns>
        public IQueryable<Tuple<ModelMemberProperty, IEnumerable<StoreMemberProperty>>> GetChangedScalarMembers()
        {
            return GetChangedScalarMembers(null, null, null, true, true, true, true, true);
        }

        /// <summary>
        /// Query for retrieving scalar members with attribute differences between the conceptual model and storage model.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptEntityTypes">Enumeration of names of entity types to exclude from the comparison</param>
        /// <param name="exceptMembers">Enumeration of names of members to exclude from the comparison</param>
        /// <param name="compareCollation">Controls if collation is compared</param>
        /// <param name="compareDefaultValue">Controls if default value is compared</param>
        /// <param name="compareKeyMembership">Controls if entity key membership is compared</param>
        /// <param name="compareMemberType">Controls if member data type, nullability, length, scale/precision etc are compared</param>
        /// <param name="compareStoreGenerated">Controls if store generated pattern is compared</param>
        /// <returns>A query returning tuples of ModelMemberProperty objects paired with IEnumerable of the mapped store member properties with attribute mismatches.</returns>
        public IQueryable<Tuple<ModelMemberProperty, IEnumerable<StoreMemberProperty>>> GetChangedScalarMembers(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptEntityTypes, IEnumerable<string> exceptMembers, bool compareMemberType, bool compareKeyMembership, bool compareStoreGenerated, bool compareDefaultValue, bool compareCollation)
        {
            return _conceptualModel.EntityTypes
                    .Where(et =>
                           et.EntitySet != null
                        && (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(et.EntitySet.FullName, StringComparison.InvariantCultureIgnoreCase)))
                        && (exceptEntityTypes == null || !exceptEntityTypes.Any(ign => ign.Equals(et.FullName, StringComparison.InvariantCultureIgnoreCase))))
                    .SelectMany(mmp =>
                        mmp.MemberProperties.Where(
                            p => (exceptMembers == null || !exceptMembers.Any(im => im.Equals(mmp.FullName, StringComparison.InvariantCultureIgnoreCase)))
                             && !p.IsComplexType
                             && p.StoreMembers.Any(
                                k => (compareKeyMembership == true && k.IsKey != p.IsKey)
                               || (compareMemberType == true && !k.CSDLType.Equals(p.CSDLType))
                               || (compareStoreGenerated == true && k.StoreGeneratedPattern != p.StoreGeneratedPattern && !p.EntityType.HasBaseType && !p.EntityType.HasSubTypes) //TODO: <= make more selective. requires changes in mapping lookup...
                               || (compareDefaultValue == true && k.DefaultValue != p.DefaultValue)
                               || (compareCollation == true && k.Collation != p.Collation))
                        )
                    ).Distinct()
                    .Select(mmp => new Tuple<ModelMemberProperty, IEnumerable<StoreMemberProperty>>(
                        mmp, mmp.StoreMembers.Where(
                            smp =>
                                  (compareKeyMembership == true && smp.IsKey != mmp.IsKey)
                               || (compareMemberType == true && !smp.CSDLType.Equals(mmp.CSDLType))
                               || (compareStoreGenerated == true && smp.StoreGeneratedPattern != mmp.StoreGeneratedPattern && !mmp.EntityType.HasBaseType && !mmp.EntityType.HasSubTypes)  //TODO: <= make more selective. requires changes in mapping lookup...
                               || (compareDefaultValue == true && smp.DefaultValue != mmp.DefaultValue)
                               || (compareCollation == true && smp.Collation != mmp.Collation)
                               )))
                    .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving associations with no mapping to storage layer associations.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptAssociations">Enumeration of names of model associations to exclude from comparison</param>
        /// <returns>A query (IQueryable) of ModelAssociationSet objects.</returns>
        public IQueryable<ModelAssociationSet> GetMissingAssociations(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptAssociations)
        {
            return _conceptualModel.AssociationSets
                .Where(
                    aset => aset.FromEntitySet != null
                        && aset.ToEntitySet != null
                        && aset.StoreAssociationSet == null
                        && aset.StoreEntitySetJunction == null
                        && (exceptEntitySets == null
                          || (
                                !exceptEntitySets.Any(ign => ign.Equals(aset.FromEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             && !exceptEntitySets.Any(ign => ign.Equals(aset.ToEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             )
                           )
                        && (exceptAssociations == null
                          || !exceptAssociations.Any(ign => ign.Equals(aset.FullName, StringComparison.InvariantCultureIgnoreCase))
                           )
                )
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving associations with changed/mismatching keys.
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptAssociations">Enumeration of names of model associations to exclude from comparison</param>
        /// <returns>A query (IQueryable) of ModelAssociationSet objects.</returns>
        public IQueryable<ModelAssociationSet> GetChangedAssociations(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptAssociations)
        {
            return _conceptualModel.AssociationSets
                .Where(
                    aset => aset.FromEntitySet != null
                        && aset.ToEntitySet != null
                        && aset.StoreAssociationSet != null
                        && (exceptEntitySets == null
                          || (
                                !exceptEntitySets.Any(ign => ign.Equals(aset.FromEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             && !exceptEntitySets.Any(ign => ign.Equals(aset.ToEntitySet.FullName, StringComparison.InvariantCultureIgnoreCase))
                             )
                           )
                        && (exceptAssociations == null
                          || !exceptAssociations.Any(ign => ign.Equals(aset.FullName, StringComparison.InvariantCultureIgnoreCase))
                           )
                        && (aset.Keys.Count() > 0 && (aset.Keys.Count() != aset.StoreAssociationSet.Keys.Count()
                         || aset.StoreAssociationSet.Keys.SelectMany(k1 => k1.Item1.ModelMembers).Intersect(aset.Keys.Select(k1 => k1.Item1)).Count() != aset.Keys.Count()
                         || aset.StoreAssociationSet.Keys.SelectMany(k2 => k2.Item2.ModelMembers).Intersect(aset.Keys.Select(k2 => k2.Item2)).Count() != aset.Keys.Count()))
                )
                .AsQueryable();
        }

        /// <summary>
        /// Query for retrieving entity subtypes not mapped to the storage layer
        /// </summary>
        /// <returns>A query (IQueryable) of ModelEntityType objects with no mapping to the storage layer.</returns>
        public IQueryable<ModelEntityType> GetUnmappedEntitySubTypes()
        {
            return GetUnmappedEntitySubTypes(null, null);
        }

        /// <summary>
        /// Query for retrieving entity subtypes not mapped to the storage layer
        /// </summary>
        /// <param name="exceptEntitySets">Enumeration of names of entity sets to exclude from comparison</param>
        /// <param name="exceptEntityTypes">Enumeration of names of entity types to exclude from the comparison</param>
        /// <returns>A query (IQueryable) of ModelEntityType objects with no mapping to the storage layer.</returns>
        public IQueryable<ModelEntityType> GetUnmappedEntitySubTypes(IEnumerable<string> exceptEntitySets, IEnumerable<string> exceptEntityTypes)
        {
            return _conceptualModel.EntityTypes
                .Where(et =>
                        et.EntitySet != null
                    && (exceptEntitySets == null || !exceptEntitySets.Any(ign => ign.Equals(et.EntitySet.FullName, StringComparison.InvariantCultureIgnoreCase)))
                    && (exceptEntityTypes == null || !exceptEntityTypes.Any(ign => ign.Equals(et.FullName, StringComparison.InvariantCultureIgnoreCase))))
                .Where(
                    et => et.HasBaseType
                        && et.TopLevelBaseType != null
                        && et.TopLevelBaseType.EntitySet != null
                        && (et.TopLevelBaseType.EntitySet.EntitySetMapping == null
                          || !et.TopLevelBaseType.EntitySet.EntitySetMapping.EntityTypes.Any(t => t == et)))
                .AsQueryable();
        }
    }
}
