using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Utils;
using System;
using System.Reflection;

namespace AllTheTasques
{
	[NetworkCompatibility(CompatibilityLevel.NoNeedForSync)]
	[BepInDependency("com.bepis.r2api")]
	[BepInDependency("com.Loafe.NSFWTasque")]
	[BepInPlugin("com.VarnaScelestus.AllTheTasques", "AllTheTasques", "1.0.0")]
	public class AllTheTasques : BaseUnityPlugin
	{

		private static readonly MethodInfo m_Tasque = typeof(NSFWTasque.NSFWTasquePlugin).GetMethod("BodyCatalogInit", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

		public void Awake()
		{
			HookEndpointManager.Modify(m_Tasque, (Action<ILContext>)AddAllSkins);
		}

        private void AddAllSkins(ILContext il)
        {
			var c = new ILCursor(il);
			c.Index += 13;
            while (c.Next != null && c.Index < c.Instrs.Count - 7)
            {
				if ( c.Next.OpCode != OpCodes.Call )
                {
					c.Remove();
					//c.Next.OpCode = OpCodes.Nop;
					//c.Next.Operand = null;
                }
				else if (!c.Next.Operand.ToString().Contains("AddMage"))
				{
					c.Remove();
					//c.Next.OpCode = OpCodes.Nop;
					//c.Next.Operand = null;
				}
				else { c.Index++; }

            }
			//Logger.LogDebug(il.ToString());
		}
    }
}