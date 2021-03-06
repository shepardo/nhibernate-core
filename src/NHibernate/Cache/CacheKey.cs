using System;
using System.Runtime.Serialization;
using NHibernate.Engine;
using NHibernate.Type;

namespace NHibernate.Cache
{
	/// <summary>
	/// Allows multiple entity classes / collection roles to be 
	/// stored in the same cache region. Also allows for composite 
	/// keys which do not properly implement equals()/hashCode().
	/// </summary>
	[Serializable]
	public class CacheKey : IDeserializationCallback
	{
		private readonly object key;
		private readonly IType type;
		private readonly string entityOrRoleName;
		// hashcode may vary among processes, they cannot be stored and have to be re-computed after deserialization
		[NonSerialized]
		private int? _hashCode;
		private readonly ISessionFactoryImplementor _factory;
		private readonly string _tenantIdentifier;

		/// <summary> 
		/// Construct a new key for a collection or entity instance.
		/// Note that an entity name should always be the root entity 
		/// name, not a subclass entity name. 
		/// </summary>
		/// <param name="id">The identifier associated with the cached data </param>
		/// <param name="type">The Hibernate type mapping </param>
		/// <param name="entityOrRoleName">The entity or collection-role name. </param>
		/// <param name="factory">The session factory for which we are caching </param>
		/// <param name="tenantIdentifier"></param>
		public CacheKey(object id, IType type, string entityOrRoleName, ISessionFactoryImplementor factory, string tenantIdentifier) 
		{
			key = id;
			this.type = type;
			this.entityOrRoleName = entityOrRoleName;
			_factory = factory;
			_tenantIdentifier = tenantIdentifier;

			_hashCode = GenerateHashCode();
		}

		//Since 5.3
		[Obsolete("Use constructor with tenantIdentifier")]
		public CacheKey(object id, IType type, string entityOrRoleName, ISessionFactoryImplementor factory)
			: this(id, type, entityOrRoleName, factory, null)
		{
		}

		//Mainly for SysCache and Memcache
		public override String ToString()
		{
			// For Component the user can override ToString
			return
				string.IsNullOrEmpty(_tenantIdentifier)
					? entityOrRoleName + "#" + key
					: string.Join("#", entityOrRoleName, key, _tenantIdentifier);
		}

		public override bool Equals(object obj)
		{
			CacheKey that = obj as CacheKey;
			if (that == null) return false;
			return entityOrRoleName.Equals(that.entityOrRoleName) && type.IsEqual(key, that.key) && _tenantIdentifier == that._tenantIdentifier;
		}

		public override int GetHashCode()
		{
			// If the object is put in a set or dictionary during deserialization, the hashcode will not yet be
			// computed. Compute the hashcode on the fly. So long as this happens only during deserialization, there
			// will be no thread safety issues. For the hashcode to be always defined after deserialization, the
			// deserialization callback is used.
			return _hashCode ?? GenerateHashCode();
		}

		/// <inheritdoc />
		public void OnDeserialization(object sender)
		{
			_hashCode = GenerateHashCode();
		}

		private int GenerateHashCode()
		{
			var hashCode = type.GetHashCode(key, _factory);

			if (_tenantIdentifier != null)
			{
				hashCode = (37 * hashCode) + _tenantIdentifier.GetHashCode();
			}

			return hashCode;
		}

		public object Key
		{
			get { return key; }
		}

		public string EntityOrRoleName
		{
			get { return entityOrRoleName; }
		}
	}
}
