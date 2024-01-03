//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AVTracksScheduler
{
    using System;
    using System.Collections.Generic;
    
    public partial class md_fleet
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public md_fleet()
        {
            this.device_fleet_mapping = new HashSet<device_fleet_mapping>();
        }
    
        public int fleet_id { get; set; }
        public string sap_id { get; set; }
        public string sap_resource_id { get; set; }
        public string fleet_type { get; set; }
        public string fleet_name { get; set; }
        public Nullable<int> contractor_comp_id { get; set; }
        public string contractor_comp_name { get; set; }
        public Nullable<int> owner_id { get; set; }
        public string owner_name { get; set; }
        public Nullable<int> status { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<device_fleet_mapping> device_fleet_mapping { get; set; }
    }
}
