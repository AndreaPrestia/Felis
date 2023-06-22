namespace Felis.Client
{
	public abstract class Consume<T> 
	{
		public abstract void Process(T entity);
	}
}
