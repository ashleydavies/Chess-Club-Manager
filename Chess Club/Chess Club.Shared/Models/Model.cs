using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Chess_Club.Models
{
    public abstract class Model
    {
        /// <summary>
        /// Initializes the instance of a model, performing basic tasks such as assigning an ID and tracking creation time.
        /// </summary>
        public async Task Initialize() {
            // Get an ID for this model if we haven't got one already
            if (ID == -1) {
                // Get a new ID from database
                ID = await App.db.GetUniqueID(this.GetType());
                // Set the creation time
                ModelCreationTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Unique ID of instance of model
        /// </summary>
        public int ID = -1;
        /// <summary>
        /// Time that this instance was created
        /// </summary>
        public DateTime ModelCreationTime;
    }
}
