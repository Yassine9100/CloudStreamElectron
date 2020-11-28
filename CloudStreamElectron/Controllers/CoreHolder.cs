using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudStreamForms.Core;
namespace CloudStreamElectron.Controllers
{
	public static class CoreHolder
	{
		static Dictionary<Guid, CloudStreamCore> cores = new Dictionary<Guid, CloudStreamCore>();

		static Guid GenerateCore()
		{
			CloudStreamCore _core = new CloudStreamCore();
			Guid _guid = Guid.NewGuid();
			cores.Add(_guid, _core);
			return _guid;
		}

		public static Guid CheckGuid(string guid)
		{
			if(guid.IsClean()) {
				bool succ = Guid.TryParse(guid, out Guid g);
				if(succ) {
					return CheckGuid(g);
				}
				else {
					return GenerateCore();
				}
			}
			else {
				return GenerateCore();
			}
		}


		public static Guid CheckGuid(Guid? guid)
		{
			if (guid == null) return GenerateCore();

			var cleanGuid= (Guid)guid;
			if(cores.ContainsKey(cleanGuid)) {
				return cleanGuid;
			}
			else {
				return GenerateCore();
			}
		}

		public static CloudStreamCore GetCore(Guid g)
		{
			return cores[g];
		}

		public static CloudStreamCore GetCore(string g)
		{
			return cores[Guid.Parse(g)];
		}
	}
}
