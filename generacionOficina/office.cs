using generacionOficina;

public class sensor
{
        public string location { get; set; }
        public string id { get; set; }
        public int capacity { get; set; }
        public int updateSensor { get; set; }
        public int values { get; set; }
        
        
        public sensor()
        {
            this.location = "";
            this.id = "";
            this.capacity = 0;
            this.updateSensor = 0;
            this.values = 0;
        }
}



