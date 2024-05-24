using System.Collections;
using UnityEngine;
using System;
using System.IO;
using System.Text;

public class Log : MonoBehaviour
{
    public Rigidbody rb;
    public float dangerRadius;
    public float warningRadius;
    [Range(0.1f, 2.5f)] public float logWait;
    public LayerMask trafficLayer;

    [NonSerialized] public static string logCreationDate;
    private string _logOutputPath;
    private StreamWriter _logger;
    private Coroutine _coroutine;
    private float _startTime;
    
    private void Awake()
    {
        logCreationDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            $"My Games/Bike AR/Logs/Log_{logCreationDate}");
        CreateCSV();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        _startTime = Time.realtimeSinceStartup;
        _coroutine = StartCoroutine(CheckCollisions());
    }

    private void CreateCSV()
    {
        if (!Directory.Exists(_logOutputPath))
        {
            Directory.CreateDirectory(_logOutputPath);
        }

        _logger = new StreamWriter($"{_logOutputPath}/{logCreationDate}.csv", true, Encoding.UTF8);
        string header =
            "Tiempo_Ejecucion(HH:mm:ss:ms),Riesgo_De_Accidente,Distancia,Nombre_Vehiculo,Posicion_Vehiculo,Velocidad_Vehiculo,Posicion_Bicicleta,Velocidad_Bicicleta";
        _logger.WriteLine(header);
    }
    
    private float Velocity(Rigidbody rbVehicle)
    {
        float speed = rbVehicle.velocity.magnitude;
        float direction = Vector3.Dot(rbVehicle.velocity.normalized, transform.forward);

        return 3.6f * (direction >= 0 ? speed : -speed);
    }

    private IEnumerator CheckCollisions()
    {
        while (true)
        {
            string lineRecord;
            float distance;
            string vehicleName;
            Rigidbody VehicleRB;
            string vehiclePosition;
            float vehicleSpeed;
            string bikePosition;
            float bikeSpeed;
            string accidentRisk;

            Collider[] trafficObjects = Physics.OverlapSphere(transform.position, warningRadius, trafficLayer);
            
            if (trafficObjects.Length == 0)
            {
                bikePosition = $"{rb.position.x:F8}/{rb.position.y:F8}/{rb.position.z:F8}";
                bikeSpeed = Velocity(rb);
                TimeSpan elapsedTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup - _startTime);
                lineRecord = $@"{elapsedTime:hh\:mm\:ss\:fff},,,,,,{bikePosition},{bikeSpeed:F8}";
                _logger.WriteLine(lineRecord);
            }
            else
            {
                foreach (Collider trafficObject in trafficObjects)
                {
                    if (!trafficObject.CompareTag("Traffic")) continue;

                    if (!Physics.Raycast(transform.position, trafficObject.transform.position - transform.position,
                            out var hit, warningRadius, trafficLayer)) continue;

                    distance = hit.distance;
                    vehicleName = trafficObject.transform.parent.parent.parent.gameObject.name;
                    VehicleRB = trafficObject.attachedRigidbody;
                    vehiclePosition = $"{VehicleRB.position.x:F8}/{VehicleRB.position.y:F8}/{VehicleRB.position.z:F8}";
                    vehicleSpeed = Velocity(VehicleRB);
                    bikePosition = $"{rb.position.x:F8}/{rb.position.y:F8}/{rb.position.z:F8}";
                    bikeSpeed = Velocity(rb);
                    accidentRisk = dangerRadius < distance && distance <= warningRadius ? "Bajo" : "Alto";
                    TimeSpan elapsedTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup - _startTime);
                    lineRecord =
                        $@"{elapsedTime:hh\:mm\:ss\:fff},{accidentRisk},{distance:F8},{vehicleName},{vehiclePosition},{vehicleSpeed:F8},{bikePosition},{bikeSpeed:F8}";

                    _logger.WriteLine(lineRecord);
                }
            }
            yield return new WaitForSeconds(logWait);
        }

        yield return null;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (_logger != null && other.collider.CompareTag("Traffic"))
        {
            string vehicleName = other.gameObject.name;
            Rigidbody VehicleRB = other.collider.attachedRigidbody;
            string vehiclePosition = $"{VehicleRB.position.x:F8}/{VehicleRB.position.y:F8}/{VehicleRB.position.z:F8}";
            float vehicleSpeed = Velocity(VehicleRB);
            string bikePosition = $"{rb.position.x:F8}/{rb.position.y:F8}/{rb.position.z:F8}";
            float bikeSpeed = Velocity(rb);
            TimeSpan elapsedTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup - _startTime);
            string lineRecord =
                $@"{elapsedTime:hh\:mm\:ss\:fff},Colisión,0,{vehicleName},{vehiclePosition},{vehicleSpeed:F8},{bikePosition},{bikeSpeed:F8}";
            _logger.WriteLine(lineRecord);
        }
    }

    private void OnDestroy()
    {
        _logger?.Close();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && _coroutine != null)
        {
            StopCoroutine(_coroutine);
            _logger?.Close();
        }
        else
        {
            _logger ??= new StreamWriter($"{_logOutputPath}/{logCreationDate}.csv", true);
            _coroutine = StartCoroutine(CheckCollisions());
        }
    }

    private void OnApplicationQuit()
    {
        _logger?.Close();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rb.position, warningRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rb.position, dangerRadius);
    }
}