using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data_access_layer.model
{
    public class Student_Exam
    {
        [Key]
        public int ID { get; set; }

        public int ExamID { get; set; }
        public int StudentID { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Score { get; set; }
        public DateTime ExamDate { get; set; } 

        // Navigation properties
        [ForeignKey(nameof(ExamID))]
        public virtual Exam Exam { get; set; }

        [ForeignKey(nameof(StudentID))]
        public virtual Student Student { get; set; }
    }
}
