using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Models
{
    public class LLM_Detail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("UserId")]
        public SignUpDetail SignUpDetail { get; set; } = null!;
        public string OriginalUserQuery { get; set; }
        public string ProcessedUserQuery { get; set; }
        public string PrimaryIntent { get; set; }
        public string SpecificAction { get; set; }
        public string FileDescription { get; set; }
        public string FileTypeFilter { get; set; }
        public string ApplicationName { get; set; }
        public string SearchEngineQuery { get; set; }
        public string SystemComponentTarget { get; set; }
        public string SystemComponentValue { get; set; }
        public string TaskDescriptionForSchedule { get; set; }
        public string ScheduleDatetimeDescription { get; set; }
        public string SystemCommand { get; set; }
        public string TimeReferences { get; set; }
    }
}
