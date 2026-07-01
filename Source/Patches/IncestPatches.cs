using System.Xml;
using Verse;

namespace LuxandraLust
{
    public class PatchOperation_BloodRelationPatch : PatchOperation
    {
        public PatchOperation enabledPatch;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (LuxandraModSettings.removeRomanceRestrictions)
            {
                return enabledPatch != null && enabledPatch.Apply(xml);
            }

            // If the toggle is off, skip the patch entirely and report success
            return true;
        }
    }
}