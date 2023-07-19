using System;
namespace Topluluk.Shared.Dtos
{
	public class IdList
	{
		
		public List<string> ids { get; set; }

		public IdList(List<string> ids)
		{
			this.ids = ids;
		}

		public IdList()
		{
			ids = new List<string>();
		}
	}
}

