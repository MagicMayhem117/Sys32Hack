using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DatabaseProject
{
    [ApiController]
    [Route("[controller]")]
    public class DataController : ControllerBase
    {
        private readonly string _filePath = "data.json";

        // Load data from data.json
        private List<DataModel> LoadData()
        {
            if (!System.IO.File.Exists(_filePath))
            {
                return new List<DataModel>();
            }

            string jsonString = System.IO.File.ReadAllText(_filePath);
            try
            {
                return JsonSerializer.Deserialize<List<DataModel>>(jsonString) ?? new List<DataModel>();
            }
            catch (JsonException)
            {
                Console.WriteLine("data.json is corrupt. Starting with an empty list.");
                return new List<DataModel>();
            }
        }

        // Save data to data.json
        private void SaveData(List<DataModel> data)
        {
            string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_filePath, jsonString);
        }

        [HttpGet]
        public IEnumerable<DataModel> GetData()
        {
            return LoadData();
        }

        [HttpPost]
        public IActionResult PostData([FromBody] DataModel newData)
        {
            List<DataModel> dataList = LoadData();
            dataList.Add(newData);
            SaveData(dataList);
            return CreatedAtAction(nameof(GetData), newData);
        }
    }
}