namespace Felis.Client
{
	public abstract class Consume<T> where T : ConsumeEntity
	{
		public abstract void Process(T entity);
	}

	public abstract class ConsumeEntity
	{

	}
}
