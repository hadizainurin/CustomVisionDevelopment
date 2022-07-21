// To run using spinnaker library, need to set platform to x64 from build
using System;
using System.IO;
using System.Collections.Generic;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace CustomVisionDevelopment
{
    class Program
    {
        // Maximum number of characters that will be
        // printed out for any information retrieved from a node.
        const int MaxChars = 35;
        
        // Trigger type is specified to either software or hardware
        enum triggerType
        {
            Software,
            Hardware
        }

        static triggerType chosenTrigger = triggerType.Software;

#if DEBUG
        static int DisableHeartbeat(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            Console.WriteLine("Checking device type to see if we need to disable the camera's heartbeat...\n\n");

            //
            // Write to boolean node controlling the camera's heartbeat
            //
            // *** NOTES ***
            // This applies only to GEV cameras and only applies when in DEBUG mode.
            // GEV cameras have a heartbeat built in, but when debugging applications the
            // camera may time out due to its heartbeat. Disabling the heartbeat prevents
            // this timeout from occurring, enabling us to continue with any necessary debugging.
            // This procedure does not affect other types of cameras and will prematurely exit
            // if it determines the device in question is not a GEV camera.
            //
            // *** LATER ***
            // Since we only disable the heartbeat on GEV cameras during debug mode, it is better
            // to power cycle the camera after debugging. A power cycle will reset the camera
            // to its default settings.
            //
            IEnum iDeviceType = nodeMapTLDevice.GetNode<IEnum>("DeviceType");
            IEnumEntry iDeviceTypeGEV = iDeviceType.GetEntryByName("GigEVision");
            // We first need to confirm that we're working with a GEV camera
            if (iDeviceType != null && iDeviceType.IsReadable)
            {
                if (iDeviceType.Value == iDeviceTypeGEV.Value)
                {
                    Console.WriteLine(
                        "Working with a GigE camera. Attempting to disable heartbeat before continuing...\n\n");
                    IBool iGEVHeartbeatDisable = nodeMap.GetNode<IBool>("GevGVCPHeartbeatDisable");
                    if (iGEVHeartbeatDisable == null || !iGEVHeartbeatDisable.IsWritable)
                    {
                        Console.WriteLine(
                            "Unable to disable heartbeat on camera. Continuing with execution as this may be non-fatal...");
                    }
                    else
                    {
                        iGEVHeartbeatDisable.Value = true;
                        Console.WriteLine("WARNING: Heartbeat on GigE camera disabled for the rest of Debug Mode.");
                        Console.WriteLine(
                            "         Power cycle camera when done debugging to re-enable the heartbeat...");
                    }
                }
                else
                {
                    Console.WriteLine("Camera does not use GigE interface. Resuming normal execution...\n\n");
                }
            }
            else
            {
                Console.WriteLine("Unable to access TL device nodemap. Aborting...");
                return -1;
            }

            return 0;
        }
#endif

        int ConfigureTrigger (INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                Console.WriteLine("\n\n*** Configuring Trigger ***\n\n");
                Console.WriteLine("Noted: If software trigger is faster than frame time, then the trigger may be dropped / skipped by the camera");
                Console.WriteLine("If several frames are needed per trigger, it is best to used multi-frame mode. \n");

                if (chosenTrigger == triggerType.Software)
                {
                    Console.WriteLine("Software trigger chosen...\n");
                }
                else if (chosenTrigger == triggerType.Hardware)
                {
                    Console.WriteLine("Hardware trigger chosen...\n");
                }

                // Trigger mode is off
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsWritable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Ending...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Ending...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...");


                // Set TriggerSelector to Framestart
                IEnum iTriggerSelector = nodeMap.GetNode<IEnum>("TriggerSelector");
                if (iTriggerSelector == null || !iTriggerSelector.IsWritable)
                {
                    Console.WriteLine("Unable to set trigger selector (enum retrieval). Ending...");
                    return -1;
                }
                IEnumEntry iTriggerSelectorFrameStarts = iTriggerSelector.GetEntryByName("FrameStart");
                if (iTriggerSelectorFrameStarts == null || !iTriggerSelectorFrameStarts.IsReadable)
                {
                    Console.WriteLine("Unable to set software trigger selector (entry retrieval. Aborting... ");
                    return -1;
                }

                iTriggerSelector.Value = iTriggerSelectorFrameStarts.Value;

                Console.WriteLine("Trigger selector set to frame start...");

                // Setting trigger source to software
                IEnum iTriggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (iTriggerSource == null || !iTriggerSource.IsWritable)
                {
                    Console.WriteLine("Unable to set trigger mode (enum retrieval). Ending...");
                    return -1;
                }

                if (chosenTrigger == triggerType.Software)
                {
                    IEnumEntry iTriggerSourceSoftware = iTriggerSource.GetEntryByName("Software");
                    if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                    {
                        Console.WriteLine("Unable to set software trigger mode (entry retrieval). Ending...");
                        return -1;
                    }

                    iTriggerSource.Value = iTriggerSourceSoftware.Value;
                    Console.WriteLine("Trigger source set to software...");

                }
                else if (chosenTrigger == triggerType.Hardware)
                {
                    // Set trigger mode to hardware ('Line0')
                    IEnumEntry iTriggerSourceHardware = iTriggerSource.GetEntryByName("Line0");
                    if (iTriggerSourceHardware == null || !iTriggerSourceHardware.IsReadable)
                    {
                        Console.WriteLine("Unable to set hardware trigger mode (entry retrieval). Ending...");
                        return -1;
                    }

                    iTriggerSource.Value = iTriggerSourceHardware.Value;

                    Console.WriteLine("Trigger source set to hardware...");
                }

                // Turn the trigger mode ON
                // We are using software trigger

                IEnumEntry iTriggerModeOn = iTriggerMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("Unable to enable trigger mode (entry retrieval). Ending...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOn.Value;
                Console.WriteLine("Trigger mode is enable...");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Grab single image using trigger
        int GrabNextImageByTrigger(INodeMap nodeMap, IManagedCamera cam)
        {
            int result = 0;

            try
            {
                 // Use trigger to capture image
                 if (chosenTrigger == triggerType.Software)
                {
                    // Get User input
                    Console.WriteLine("Press Enter key to initiate software trigger.");
                    Console.ReadLine();

                    // Execute software trigger
                    ICommand iTriggerSoftware = nodeMap.GetNode<ICommand>("TriggerSoftware");
                    if (iTriggerSoftware == null || !iTriggerSoftware.IsWritable)
                    {
                        Console.WriteLine("Unable to execute trigger. Ending...");
                        return -1;
                    }

                    iTriggerSoftware.Execute();

                    // Note: Blackfly and Flea3 GEV cameras need 2 second delay after software trigger
                }
                 else if (chosenTrigger == triggerType.Hardware)
                {
                    // Execute hardware trigger
                    Console.WriteLine("Use the hardware to trigger image acquisition.");
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // Reset the camera to normal state by turning off trigger mode

        int ResetTrigger(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                //
                // Turn trigger mode back off
                //
                // *** NOTES ***
                // Once all images have been captured, turn trigger mode back
                // off to restore the camera to a clean state.
                //
                IEnum iTriggerMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (iTriggerMode == null || !iTriggerMode.IsWritable)
                {
                    Console.WriteLine("Unable to disable trigger mode (enum retrieval). Non-fatal error...");
                    return -1;
                }

                IEnumEntry iTriggerModeOff = iTriggerMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable trigger mode (entry retrieval). Non-fatal error...");
                    return -1;
                }

                iTriggerMode.Value = iTriggerModeOff.Value;

                Console.WriteLine("Trigger mode disabled...\n");
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }
        int ConfigureExposure(INodeMap nodeMap)
        {
            int result = 0;
            Console.WriteLine("\n\n*** Configuring Exposure ***\n");

            try
            {
                // Turn off automatic exposure mode
                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (iExposureAuto == null || !iExposureAuto.IsWritable)
                {
                    Console.WriteLine("Unable to disable automatic exposure (enum retrieval). Aborting...\n");
                    return -1;
                }

                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
                {
                    Console.WriteLine("Unable to disable automatic exposure (entry retrieval). Aborting...\n");
                    return -1;
                }

                // What the difference between Symbolic and Value?
                iExposureAuto.Value = iExposureAutoOff.Value;

                // Set exposure time manually in microseconds
                const double exposureTimeToSet = 123040.0;

                // This IFloat is to get the information
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                if (iExposureTime == null || !iExposureTime.IsWritable)
                {
                    Console.WriteLine("Unable to set exposure time. Aborting...\n");
                    return -1;
                }

                // Ensure desire exposure time does not exceed maximum value
                iExposureTime.Value = (exposureTimeToSet > iExposureTime.Max ? iExposureTime.Max : exposureTimeToSet);

                // Add .Unit to get the value current unit, by default is in us
                Console.WriteLine("Exposure time set to {0} {1} ...\n",iExposureTime.Value,iExposureTime.Unit);
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }
        int AcquireImages(IManagedCamera cam, INodeMap nodeMap, INodeMap nodeMapTLDevice)
        {
            int result = 0;
            Console.WriteLine("\n*** CAPTURING IMAGE ***\n");

            try
            {
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable)
                {
                    Console.WriteLine("Unable to retrieve node). Aborting...\n");
                    return -1;
                }

                // This is where we change the Acquisition Mode setting
                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionMode.IsReadable)
                {
                    Console.WriteLine("Unable to retrive enum entry. Aborting...\n");
                    return -1;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;
                Console.WriteLine("Acquisition mode sucessfully set to continuous...");

#if DEBUG
                Console.WriteLine("\n\n*** DEBUG ***\n\n");
                // If using a GEV camera and debugging, should disable heartbeat first to prevent further issues

                if (DisableHeartbeat(cam, nodeMap, nodeMapTLDevice) != 0)
                {
                    return -1;
                }

                Console.WriteLine("\n\n*** END OF DEBUG ***\n\n");
#endif

                cam.BeginAcquisition();
                Console.WriteLine(" Capturing Images...");
                // Acquiring Image
                String deviceSerialNumber = "";

                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
                {
                    deviceSerialNumber = iDeviceSerialNumber.Value;
                    Console.WriteLine("Device serial number retrieved as {0}...", deviceSerialNumber);
                }
                Console.WriteLine();
                // Get 10 images
                const int NumImages = 10;
                
                // ImageProcessor instance for post processing image
                IManagedImageProcessor processor = new ManagedImageProcessor();
                // Set default image processor color processing method here
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);

                for (int imageCnt = 0; imageCnt < NumImages; imageCnt++)
                {
                    try
                    {
                        // Retrieve next received image and ensure image completion
                        result = result | GrabNextImageByTrigger(nodeMap, cam);

                        using (IManagedImage rawImage = cam.GetNextImage(1000))
                        {
                            // Check if images acquisition is finished
                            if (rawImage.IsIncomplete)
                            {
                                Console.WriteLine("Image incomplete with image status {0}...", rawImage.ImageStatus);
                            }
                            else
                            {
                                //Console.WriteLine("Grabbed image {0}, width = {1}, height = {2}",
                                //    imageCnt,
                                //    rawImage.Width,
                                //    rawImage.Height);

                                uint width = rawImage.Width; // This is a lot simpler than the method above
                                uint height = rawImage.Height;

                                Console.WriteLine("Grabbed image {0}, width = {1}, height = {2}", imageCnt, width, height);
                                // Convert image to mono 8 (Try without its first)
                                // using (IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                                
                                // Create a unique filename
                                String filename = "Custom-Vision-";
                                if (deviceSerialNumber != "")
                                {
                                    filename = filename + deviceSerialNumber + "-";
                                }
                                filename = filename + imageCnt + ".jpg";
                                rawImage.Save(filename);
                                Console.WriteLine("Image saved at {0}\n", filename);
                            }
                        }
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }
                }
                // End acquisition
                cam.EndAcquisition();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This is crucial step to get device information
        static int PrintDeviceInfo(INodeMap nodeMap)
        {
            int result = 0;

            try
            {
                Console.WriteLine("\n*** DEVICE INFO ***\n");

                ICategory category = nodeMap.GetNode<ICategory>("DeviceInformation");
                if (category != null && category.IsReadable)
                {
                    for (int i = 0; i < category.Children.Length; i++)
                    {
                        Console.WriteLine(
                            "{0}: {1}",
                            category.Children[i].Name,
                            (category.Children[i].IsReadable ? category.Children[i].ToString()
                            : "Node not available"));
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Device control information is unavailable");
                }
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }
            return result;
        }

        // This is crucial step to ensure the camera can be configured
        int RunSingleCamera(IManagedCamera cam)
        {
            int result = 0;
            int err = 0; // Exposure
            int trg = 0; // Trigger
            //int level = 0;

            try
            {
                // Retrieve TL device nodemap and print device information
                // *** NOTES ***
                // The TL device nodemap is available on the transport layer.
                // Therefore, camera initialization is unnecessary
                // It provides mostly inmmutable information fundamental to the
                // camera such as the serial number, vendor and model.

                Console.WriteLine("\n*** Printing TL Device Nodemap ***\n");

                INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();

                result = PrintDeviceInfo(nodeMapTLDevice);
                // result = printCategoryNodeAndAllFeatures(nodeMapTLDevice.GetNode<ICategory>("Root"), level);

                // Print TL stream nodemap
                INodeMap nodeMapTLStream = cam.GetTLStreamNodeMap();

                // result = result | printCategoryNodeAndAllFeatures(nodeMapTLSream.GetNode<ICategory>("Root"), level);

                // Initialize camera
                // This is how we connected to the camera
                cam.Init();

                // Retrieve GenICam nodemap
                // Primary gateway to customising and configuring the camera to necessary setting
    
                INodeMap nodeMap = cam.GetNodeMap();

                // Print GenICam nodemap
                //result = result | printCategoryNodeAndAllFeatures(appLayerNodeMap.GetNode<ICategory>("Root"), level);

                // Configure Exposure
                err = ConfigureExposure(nodeMap);
                if (err <0)
                {
                    return err;
                }

                // Configure Trigger
                trg = ConfigureTrigger(nodeMap);
                if (trg < 0)
                {
                    return trg;
                }

                // Acquire images
                result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                // Reset Trigger
                
                result = result | ResetTrigger(nodeMap);

                // Can add reset exposure but not needed

                // Deinitialize camera
                // Ensures the devices clean up properly
                // And avoid power-cycled to maintain integrity
                cam.DeInit();

                // Dispose of camera
                //cam.DIspose();
            }
            catch (SpinnakerException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                result = -1;
            }

            return result;
        }

        // Entry point
        static int Main(string[] args)
        {
            int result = 0;

            Program program = new Program();

            FileStream fileStream;
            try
            {
                fileStream = new FileStream(@"test.txt", FileMode.Create);
                fileStream.Close();
                File.Delete("test.txt");
            }
            catch
            {
                Console.WriteLine("Failed to create file in current folder. Please check permissions.");
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
                return -1;
            }

            // Retrieve singleton reference to systme object
            ManagedSystem system = new ManagedSystem();

            LibraryVersion spinVersion = system.GetLibraryVersion();
            Console.WriteLine(
                "Spinnaker Library version: {0}.{1}.{2}.{3}\n",
                spinVersion.major,
                spinVersion.minor,
                spinVersion.type,
                spinVersion.build);

            // Retrieve list of cameras from the system
            ManagedCameraList camList = system.GetCameras();

            Console.WriteLine("Number of cameras detected: {0}\n\n", camList.Count);

            // Finish if no camera
            if (camList.Count == 0)
            {
                // Clear camera list before releasing system
                camList.Clear();

                // Release system
                system.Dispose();

                Console.WriteLine("Not enough cameras!");
                Console.WriteLine("Done! Press Enter to exit...");
                Console.ReadLine();

                return -1;
            }

            int index = 0;

            foreach (IManagedCamera managedCamera in camList) using (managedCamera)
                {
                    Console.WriteLine("Running example for camera {0}...", index);

                    try
                    {
                        // RunSingleCamera will is configure to to acquire image
                        // See example AcquisitionCSharp and Exposure for different setup

                        result = result | program.RunSingleCamera(managedCamera);
                    }
                    catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }

                    Console.WriteLine("Camera {0} example complete...\n", index++);
                }

            camList.Clear();
            system.Dispose();
            Console.WriteLine("All processed are completed. Please press enter to exit...");
            Console.ReadLine();

            return result;
        }
    }
}
