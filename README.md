# IoT Simulator Exercise 
An IoT simulator, consisted from device, gateway and controller that connect via rabbitmq.  
The flow of messages:  
`Device -> (DeviceStatusQueue) -> Gateway -> (ContollerStatusQueue) -> Controller`   
`-> (ControllerCommandQueue) -> Gateway -> (GatewayCommandQueue) -> Device.`  
  
The gateway is used to transfer messages between device and controller,  
and thus subscribes to both DeviceStatusQueue and ControllerCommandQueue.  
  
To start the project:  
`Start the device alone, then the gateway and then the controller (all on debug).`  
This will create the queues with the desired configuration and avoid error messages on apps.  
After the queues are established, need to purge them, and then it is possible to start the entire project for debug  
(multiple startup projects on solution).  
