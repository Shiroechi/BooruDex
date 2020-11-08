using BooruDex.Booru.Template;

namespace BooruDex.Booru.Client
{
	public class Lolibooru : Moebooru
	{
		public Lolibooru() : base("http://lolibooru.moe/")
		{
			// lolibooru tag limit can more than 6
		}
	}
}
