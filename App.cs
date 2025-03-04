// Referenece https://www.youtube.com/watch?v=lYjHDkpbsas 

#region Namespaces
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;


#endregion

namespace RevitWoodLCC
{
    class App : IExternalApplication
    {

        const string TAB_NAME = "RevitWoodLCC";
        const string PANEL1_NAME = "Parameters Manager";//"IFC Extension and Parameters Manager";
        const string PANEL2_NAME = "Service Life Calculations";
        const string PANEL3_NAME = "LCC Calculations and Visualization";
        //const string PANEL4_NAME = "Weathering Dose";//"Results and Visualization"
        const string PANEL5_NAME = "About";
     //const string PANEL6_NAME = "Code Testing";
        // private object button;

        public Result OnStartup(UIControlledApplication a)
        {
            //get the ribbon tab
            try
            {
                a.CreateRibbonTab(TAB_NAME);
            }
            catch (Exception) { } // tab already exist


            //START OF PANEL 1
            //get or create PANELS
            RibbonPanel panel1 = null;
            List<RibbonPanel> panels1 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl1 in panels1)
            {
                if (pnl1.Name == PANEL1_NAME)
                {
                    panel1 = pnl1;
                    break;
                }
            }


            //couldnt find the panel, create it
            if (panel1 == null)
            {
                panel1 = a.CreateRibbonPanel(TAB_NAME, PANEL1_NAME);
            }

            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            PushButtonData panel1_Pushbtn1 = new PushButtonData("MyButton1", "Add WoodLCC Parameters", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.AddSharedParameters")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/AddParameter30x30_100.png"))
            };
            // Add the push buttons to the ribbon panel
            PushButton add_panel1_Pushbtn1 = panel1.AddItem(panel1_Pushbtn1) as PushButton;

            PushButtonData panel1_Pushbtn2 = new PushButtonData("MyButton2", "Set Project Location", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.SetProjectLocation") //RevitWoodLCC.Command_AddParameters.SetProjectLocationUpdate.
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            };
            // Add the push buttons to the ribbon panel
            PushButton add_panel1_Pushbtn2 = panel1.AddItem(panel1_Pushbtn2) as PushButton;

            PushButtonData panel1_Pushbtn3 = new PushButtonData("MyButton3", "Material Manager", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ImportMaterialsCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            };
            // Add the push buttons to the ribbon panel
            PushButton add_panel1_Pushbtn3 = panel1.AddItem(panel1_Pushbtn3) as PushButton;

            //PushButtonData panel1_Pushbtn4 = new PushButtonData("MyButton4", "Virtual Inspection", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RayCastingFaceIntersection")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/InspectWood30x30.png"))
            //};
            //// Add the push buttons to the ribbon panel
            //PushButton add_panel1_Pushbtn4 = panel1.AddItem(panel1_Pushbtn4) as PushButton;

          


            /*
            //STACKED BUTTONS
            PushButtonData panel1_Stackbtn1 = new PushButtonData("MyStackButton1", "Select all Wooden Elements", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.DisplaySelected")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/Wood30x30.png"))
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/SelectWood10x10.png"))
            };

            PushButtonData panel1_Stackbtn2 = new PushButtonData("MyStackButton2", "Virtual Inspection", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/InspectWood10x10.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };

            PushButtonData panel1_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };
            panel1.AddStackedItems(panel1_Stackbtn1, panel1_Stackbtn2, panel1_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc
            */

            //END OF PANEL 1

            //START OF PANEL 2
            //get or create PANELS
            RibbonPanel panel2 = null;
            List<RibbonPanel> panels2 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl2 in panels2)
            {
                if (pnl2.Name == PANEL2_NAME)
                {
                    panel2 = pnl2;
                    break;
                }
            }

            //couldnt find the panel, create it
            if (panel2 == null)
            {
                panel2 = a.CreateRibbonPanel(TAB_NAME, PANEL2_NAME);
            }

            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            PushButtonData panel2_Pushbtn1 = new PushButtonData("Service_Life_Estimation", "Service Life\n Estimation", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ServiceLifeEstimation")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/ServiceLife30x30.png"))
            };
            PushButton add_panel2_Pushbtn1 = panel2.AddItem(panel2_Pushbtn1) as PushButton;

            PushButtonData panel2_Pushbtn2 = new PushButtonData("Automatic_Service_Life_Estimation", "Automatic Service\n Life Estimation", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.AutoEstimateServiceLife")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/ServiceLife30x30.png"))
            };
            PushButton add_panel2_Pushbtn2 = panel2.AddItem(panel2_Pushbtn2) as PushButton;

            //PushButtonData panel2_Pushbtn3 = new PushButtonData("Visualize_SLE_Results", "Visualize SLE Results", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ApplyColorToElements")//ApplyAVFtoFaces  SLE_Results_Visualization
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/ServiceLife30x30.png"))
            //};
            //PushButton add_panel2_Pushbtn3 = panel2.AddItem(panel2_Pushbtn3) as PushButton;

            PushButtonData panel2_Pushbtn4 = new PushButtonData(
                "Visualize_SLE_Results",
                "Visualize SLE Results",
                Assembly.GetExecutingAssembly().Location,
                "RevitWoodLCC.SetElementSurfaceColorCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/ServiceLife30x30.png"))
            };
            PushButton add_panel2_Pushbtn4 = panel2.AddItem(panel2_Pushbtn4) as PushButton;


            /*
            //STACKED BUTTONS
            PushButtonData panel2_Stackbtn1 = new PushButtonData("MyStackButton1","My Stack Ribbon Button 1",Assembly.GetExecutingAssembly().Location,"RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/AddParameter_32.png"))
                //LargeImage = panel1_Stackbtn1_icon
            };

            PushButtonData panel2_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/InspectWood50x50.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };
            
            PushButtonData panel2_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };
            panel2.AddStackedItems(panel2_Stackbtn1, panel2_Stackbtn2, panel2_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc
            */

            //END OF PANEL 2


            //START OF PANEL 3
            //get or create PANELS
            RibbonPanel panel3 = null;
            List<RibbonPanel> panels3 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl3 in panels3)
            {
                if (pnl3.Name == PANEL3_NAME)
                {
                    panel3 = pnl3;
                    break;
                }
            }

            //couldnt find the panel, create it
            if (panel3 == null)
            {
                panel3 = a.CreateRibbonPanel(TAB_NAME, PANEL3_NAME);
            }

            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            PushButtonData panel3_Pushbtn1 = new PushButtonData("MyButton1", "LCC", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.LifeCycleCosting")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/LCC30X30.png"))
            };
            PushButton add_panel3_Pushbtn1 = panel3.AddItem(panel3_Pushbtn1) as PushButton;

            PushButtonData panel3_Pushbtn2 = new PushButtonData("MyButton2", "Project LCC", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.OverallLifeCycleCosting")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/Results30x30.png"))
            };
            PushButton add_panel3_Pushbtn2 = panel3.AddItem(panel3_Pushbtn2) as PushButton;

            PushButtonData panel3_Pushbtn3 = new PushButtonData("MyButton3", "View Results", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.EndgrainSidegrain")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/Results30x30.png"))
            };
            PushButton add_panel3_Pushbtn3 = panel3.AddItem(panel3_Pushbtn3) as PushButton;



            //SplitButtonData panel3_PushSplitbtn = new SplitButtonData("splitButton1", "Split");
            //SplitButton panel3_PushDropDownbtns = panel3.AddItem(panel3_PushSplitbtn) as SplitButton;



            //STACKED BUTTONS
            /*
            PushButtonData panel3_Stackbtn1 = new PushButtonData("MyStackButton1", "My Stack Ribbon Button 1", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon50X50.png"))
                //LargeImage = panel1_Stackbtn1_icon
            };

            PushButtonData panel3_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };

            PushButtonData panel3_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
                //LargeImage = panel1_Stackbtn2_icon
            };
            panel3.AddStackedItems(panel3_Stackbtn1, panel3_Stackbtn2, panel3_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc
            */
            //END OF PANEL 3


            ////START OF PANEL 4
            ////get or create PANELS
            //RibbonPanel panel4 = null;
            //List<RibbonPanel> panels4 = a.GetRibbonPanels(TAB_NAME);
            //foreach (RibbonPanel pnl4 in panels4)
            //{
            //    if (pnl4.Name == PANEL4_NAME)
            //    {
            //        panel4 = pnl4;
            //        break;
            //    }
            //}

            ////couldnt find the panel, create it
            //if (panel4 == null)
            //{
            //    panel4 = a.CreateRibbonPanel(TAB_NAME, PANEL4_NAME);
            //}

            ////create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            //PushButtonData panel4_Pushbtn1 = new PushButtonData("MyButton", "Wind Driven Rain", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.PickedFaceRayProjectionCode_new")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};
            //PushButton add_panel4_Pushbtn1 = panel4.AddItem(panel4_Pushbtn1) as PushButton;

            ////STACKED BUTTONS
            ////PushButtonData panel4_Stackbtn1 = new PushButtonData("MyStackButton1", "Floor-Ceiling Distance", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.CheckElementType")
            ////{
            ////    ToolTip = "Add Button Description that is shown when hovered over the button",
            ////    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            ////    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon50X50.png"))
            ////    //Image = imgSrc,
            ////    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            ////};

            ////PushButtonData panel4_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.PickedFaceRayProjectionCode_new")
            ////{
            ////    ToolTip = "Add Button Description that is shown when hovered over the button",
            ////    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            ////    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
            ////    //Image = imgSrc,
            ////    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            ////};

            ////PushButtonData panel4_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ApplyAVFToAllFaces")//ApplyAVFToAllFace
            ////{
            ////    ToolTip = "Add Button Description that is shown when hovered over the button",
            ////    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            ////    //Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
            ////    //Image = imgSrc,
            ////    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            ////};



            ////panel4.AddStackedItems(panel4_Stackbtn1, panel4_Stackbtn2, panel4_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc

            ////END OF PANEL 4


            //START OF PANEL 5
            //get or create PANELS
            RibbonPanel panel5 = null;
            List<RibbonPanel> panels5 = a.GetRibbonPanels(TAB_NAME);
            foreach (RibbonPanel pnl5 in panels5)
            {
                if (pnl5.Name == PANEL5_NAME)
                {
                    panel5 = pnl5;
                    break;
                }
            }

            //couldnt find the panel, create it
            if (panel5 == null)
            {
                panel5 = a.CreateRibbonPanel(TAB_NAME, PANEL5_NAME);
            }

            //create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined and added to ribbon panel 5
            PushButtonData panel5_Pushbtn1 = new PushButtonData("MyButton1", "References and Info for users", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.AboutCommand")
            {
                ToolTip = "Add Button Description that is shown when hovered over the button",
                LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
                //Image = imgSrc,
                LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/WoodLCC Logo30x30.png"))
            };


            //PushButtonData panel5_Pushbtn2 = new PushButtonData("MyButton2", "WebApplication", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.DrawFourLinesFromMidpoint")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel5_Pushbtn3 = new PushButtonData("MyButton3", "IoT", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.DisplayTimeCommand")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};


            // Add the push buttons to the ribbon panel
            PushButton add_panel5_Pushbtn1 = panel5.AddItem(panel5_Pushbtn1) as PushButton;
            // PushButton add_panel5_Pushbtn2 = panel5.AddItem(panel5_Pushbtn2) as PushButton;
            //PushButton add_panel5_Pushbtn3 = panel5.AddItem(panel5_Pushbtn3) as PushButton;


            //END OF PANEL 5


            //////START OF PANEL 6
            //////get or create PANELS
            //RibbonPanel panel6 = null;
            //List<RibbonPanel> panels6 = a.GetRibbonPanels(TAB_NAME);
            //foreach (RibbonPanel pnl6 in panels6)
            //{
            //    if (pnl6.Name == PANEL6_NAME)
            //    {
            //        panel6 = pnl6;
            //        break;
            //    }
            //}

            ////couldnt find the panel, create it
            //if (panel6 == null)
            //{
            //    panel6 = a.CreateRibbonPanel(TAB_NAME, PANEL6_NAME);
            //}


            ////create the button data Dinifition. Here the characteristics of each button in the variaous panaels are defined
            //PushButtonData panel6_Pushbtn1 = new PushButtonData("MyTestButton1", "GetFaceRefFromElements", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.CreateMaterials")// GetFaceRefFromElements
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/LCC30X30.png"))
            //};
            //PushButton add_panel6_Pushbtn1 = panel6.AddItem(panel6_Pushbtn1) as PushButton;

            //PushButtonData panel6_Pushbtn2 = new PushButtonData("MyTestButton2", "AVFtoSelectedElement", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.AVFtoSelectedElement")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/Results30x30.png"))
            //};
            //PushButton add_panel6_Pushbtn2 = panel6.AddItem(panel6_Pushbtn2) as PushButton;


            //////-------------------------------TEST-----------------------------------
            ////// Create PullDown Button
            //PushButtonData panel6_PushDropDownbtn1 = new PushButtonData("PullTestButton1", "CheckElementType", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.CheckElementType")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn2 = new PushButtonData("MyTestDropDownButton2", "test3DPreview", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.test3DPreviewXAML")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn3 = new PushButtonData("MyTestDropDownButton3", "SelectFaceAddAVF", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ApplyAVFToFace")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn4 = new PushButtonData("MyTest_DropDownButton4", "AddButtonName", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RayCastingFaceIntersection")//CreateDummyGeometry,RayCastingFaceIntersection, GeometryUtility, SolidIntersect
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};


            //PushButtonData panel6_PushDropDownbtn5 = new PushButtonData("TestButton5", "My Ribbon Button", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RayCasting")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn6 = new PushButtonData("MyDropDownButton6", "ListContactFacesCommand", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RayIntersectCommand")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn7 = new PushButtonData("MyDropDownButton7", "SolidIntersectNEW", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ColorPickedFacesCommand")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn8 = new PushButtonData("MyDropDownButton8", "SolidIntersect", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RayCastingFaceIntersection")//CreateDummyGeometry,RayCastingFaceIntersection, GeometryUtility, SolidIntersect
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn9 = new PushButtonData("InteractiveMap", "InteractiveMap", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ShowWebViewCommand")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn10 = new PushButtonData("MyDropDownButton10", "ShowMapViewCommand", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ShowInteractiveMapCommand") //RevitWoodLCC.Command_AddParameters.SetProjectLocationUpdate.InteractiveMapWindow.ShowInteractiveMapCommand
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn11 = new PushButtonData("MyDropDownButton11", "RaindropAVFApplication", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.RaindropAVFApplication")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn12 = new PushButtonData("MyDropDownButton12", "ElementLoopSelector", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.ElementLoopSelector")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn13 = new PushButtonData("MyDropDownButton13", "FindAndHighlightElementsWithoutMaterials", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.FindAndHighlightElementsWithoutMaterials")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn14 = new PushButtonData("MyDropDownButton14", "SetMaterialClassCommand", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.SetMaterialClassCommand")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn15 = new PushButtonData("MyDropDownButton15", "AVFapplyColorToFace ", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.CreateAnalysisDisplayTypeRanges")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //PushButtonData panel6_PushDropDownbtn16 = new PushButtonData("MyDropDownButton16", "CreateElement", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.CreateElement")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    //Image = imgSrc,
            //    LargeImage = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/IFC30X30.png"))
            //};

            //SplitButtonData panel6_PushSplitbtn = new SplitButtonData("splitButton1", "Split");
            //SplitButton panel6_PushDropDownbtns = panel6.AddItem(panel6_PushSplitbtn) as SplitButton;

            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn1);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn2);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn3);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn4);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn5);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn6);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn7);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn8);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn9);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn10);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn11);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn12);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn13);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn14);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn15);
            //panel6_PushDropDownbtns.AddPushButton(panel6_PushDropDownbtn16);

            //////-------------------------------END_TEST-----------------------------------


            ////STACKED BUTTONS
            ///*
            //PushButtonData panel3_Stackbtn1 = new PushButtonData("MyStackButton1", "My Stack Ribbon Button 1", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon50X50.png"))
            //    //LargeImage = panel1_Stackbtn1_icon
            //};

            //PushButtonData panel3_Stackbtn2 = new PushButtonData("MyStackButton2", "My Stack Ribbon Button 2", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
            //    //LargeImage = panel1_Stackbtn2_icon
            //};

            //PushButtonData panel3_Stackbtn3 = new PushButtonData("MyStackButton3", "My Stack Ribbon Button 3", Assembly.GetExecutingAssembly().Location, "RevitWoodLCC.Command")
            //{
            //    ToolTip = "Add Button Description that is shown when hovered over the button",
            //    LongDescription = "Add Longer Description to show when you hover over the button for a few seconds",
            //    Image = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/icon30X30.png")),
            //    //LargeImage = panel1_Stackbtn2_icon
            //};
            //panel3.AddStackedItems(panel3_Stackbtn1, panel3_Stackbtn2, panel3_Stackbtn3); //add the button to the ribbon. Here I defined whether its going to be push button, stack etc
            //*/
            ////END OF PANEL 6


            // ENDING LINE FOR APPLICATION INTERFACE
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
