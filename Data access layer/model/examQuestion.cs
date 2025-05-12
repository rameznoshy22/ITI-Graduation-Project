using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_access_layer.model
{
    public class examQuestion
    {
        [Key]
        public int ID { get; set; }

        public decimal mark { get; set; }=0 ;
        public int ExamID { get; set; }
        public int QuestionID { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ExamID))]
        public virtual Exam Exam { get; set; }

        [ForeignKey(nameof(QuestionID))]
        public virtual Questions Question { get; set; }
        public virtual ICollection<student_answers> Answers { get; set; } = new HashSet<student_answers>();
    }
}
