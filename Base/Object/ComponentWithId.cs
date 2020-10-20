using MongoDB.Bson.Serialization.Attributes;

namespace ETModel
{
	[BsonIgnoreExtraElements]
	public abstract class ComponentWithId : Component
	{
		[BsonIgnoreIfDefault]
		[BsonDefaultValue(0L)]
		[BsonElement]
		[BsonId]
		public long Id { get; set; }

		protected ComponentWithId()
		{
		}

		protected ComponentWithId(long id)
		{
			this.Id = id;
		}

	}
}