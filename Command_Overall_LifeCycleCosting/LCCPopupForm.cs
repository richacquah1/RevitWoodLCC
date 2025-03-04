using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public class LccPopupForm : Form
{
    private Label materialLabel;
    private Label initialCostLabel;
    private Label maintenanceCostLabel;
    private Label replacementCostLabel;
    private Label serviceLifeLabel;
    private Label otherCostsLabel;
    private Label discountRateLabel;
    private Label residualValueLabel;
    private Label currencyLabel;
    private Label locationLabel;

    private TextBox materialField;
    private TextBox initialCostField;
    private TextBox maintenanceCostField;
    private TextBox replacementCostField;
    private TextBox serviceLifeField;
    private TextBox otherCostsField;
    private TextBox discountRateField;
    private TextBox residualValueField;
    private TextBox currencyField;
    private TextBox locationField;

    //private CheckBox materialCheckBox;
    private CheckBox initialCostCheckBox;
    private CheckBox maintenanceCostCheckBox;
    private CheckBox replacementCostCheckBox;
    private CheckBox serviceLifeCheckBox;
    private CheckBox residualValueCheckBox;
    private CheckBox discountRateCheckBox;
    private CheckBox otherCostsCheckBox;
    //private CheckBox currencyCheckBox;

    private Button calculateButton;

    private Button previousButton;
    private Button nextButton;

    private Label lccOutputLabel;  // Output label for the LCC result
    private TextBox lccOutputField;  // Output field for the LCC result
    private Button saveButton;  // Save button


    public LccPopupForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Set the form properties
        this.Text = "Life Cycle Cost Calculation";
        this.Size = new Size(420, 400);

        // Initialize the labels
        materialLabel = new Label();
        locationLabel = new Label();
        initialCostLabel = new Label();
        maintenanceCostLabel = new Label();
        replacementCostLabel = new Label();
        serviceLifeLabel = new Label();
        otherCostsLabel = new Label();
        discountRateLabel = new Label();
        residualValueLabel = new Label();
        currencyLabel = new Label();

        // Set the label texts
        materialLabel.Text = "Material:";
        locationLabel.Text = "Location:";
        initialCostLabel.Text = "Initial Cost:";
        maintenanceCostLabel.Text = "Maintenance Costs (plus labour):";
        replacementCostLabel.Text = "Replacement Costs (plus labour):";
        serviceLifeLabel.Text = "Service Life Duration (in months):";
        otherCostsLabel.Text = "Other Costs:";
        discountRateLabel.Text = "Discount Rate:";
        residualValueLabel.Text = "Residual Value:";
        currencyLabel.Text = "Add Currency:";

        // Initialize the input fields
            materialField = new TextBox();
            locationField = new TextBox();
            initialCostField = new TextBox();
            maintenanceCostField = new TextBox();
            replacementCostField = new TextBox();
            serviceLifeField = new TextBox();
            otherCostsField = new TextBox();
            discountRateField = new TextBox();
            residualValueField = new TextBox();
            currencyField = new TextBox();

            // Set the location and size for each control
            int labelX = 30;
            int fieldX = 240;
            int startY = 10;
            int spacing = 30;

            materialLabel.Location = new Point(labelX, startY);
            materialLabel.AutoSize = true;
            materialField.Location = new Point(fieldX, startY);
            materialField.Size = new Size(100, 20);

            locationLabel.Location = new Point(labelX, startY + spacing);
            locationLabel.AutoSize = true;
            locationField.Location = new Point(fieldX, startY + spacing);
            locationField.Size = new Size(100, 20);

            initialCostLabel.Location = new Point(labelX, startY + spacing * 2);
            initialCostLabel.AutoSize = true;
            initialCostField.Location = new Point(fieldX, startY + spacing * 2);
            initialCostField.Size = new Size(100, 20);

            maintenanceCostLabel.Location = new Point(labelX, startY + spacing * 3);
            maintenanceCostLabel.AutoSize = true;
            maintenanceCostField.Location = new Point(fieldX, startY + spacing * 3);
            maintenanceCostField.Size = new Size(100, 20);

            replacementCostLabel.Location = new Point(labelX, startY + spacing * 4);
            replacementCostLabel.AutoSize = true;
            replacementCostField.Location = new Point(fieldX, startY + spacing * 4);
            replacementCostField.Size = new Size(100, 20);

            serviceLifeLabel.Location = new Point(labelX, startY + spacing * 5);
            serviceLifeLabel.AutoSize = true;
            serviceLifeField.Location = new Point(fieldX, startY + spacing * 5);
            serviceLifeField.Size = new Size(100, 20);

            otherCostsLabel.Location = new Point(labelX, startY + spacing * 6);
            otherCostsLabel.AutoSize = true;
            otherCostsField.Location = new Point(fieldX, startY + spacing * 6);
            otherCostsField.Size = new Size(100, 20);

            discountRateLabel.Location = new Point(labelX, startY + spacing * 7);
            discountRateLabel.AutoSize = true;
            discountRateField.Location = new Point(fieldX, startY + spacing * 7);
            discountRateField.Size = new Size(100, 20);

            residualValueLabel.Location = new Point(labelX, startY + spacing * 8); // Adjust the location as per your layout
            residualValueLabel.AutoSize = true;
            residualValueField.Location = new Point(fieldX, startY + spacing * 8); // Adjust the location as per your layout
            residualValueField.Size = new Size(100, 20);

            currencyLabel.Location = new Point(labelX, startY + spacing * 9); // Adjust the location as per your layout
            currencyLabel.AutoSize = true;
            currencyField.Location = new Point(fieldX, startY + spacing * 9); // Adjust the location as per your layout
            currencyField.Size = new Size(100, 20);

            // Initialize the LCC output label
            lccOutputLabel = new Label();
            lccOutputLabel.Text = "Life Cycle Cost:";
            lccOutputLabel.AutoSize = true;
            lccOutputLabel.Location = new Point(labelX, startY + spacing * 10);

            // Initialize the LCC output field
            lccOutputField = new TextBox();
            lccOutputField.Location = new Point(fieldX, startY + spacing * 10);
            lccOutputField.Size = new Size(100, 20);
            lccOutputField.ReadOnly = true;  // So the user can't edit it


            // Initialize the calculate button
            calculateButton = new Button();
            calculateButton.Text = "Calculate";
            calculateButton.Location = new Point(fieldX  /*+ 120*/, startY + spacing * 11);
            //calculateButton.Location = new Point(fieldX, startY + spacing * 6);
            calculateButton.Size = new Size(100, 30);
            calculateButton.Click += CalculateButton_Click;

            // Initialize the save button
            saveButton = new Button();
            saveButton.Text = "Save Results";
            saveButton.Location = new Point(fieldX /*+ 120*/, startY + spacing * 12);
            saveButton.Size = new Size(100, 30);
            saveButton.Click += SaveButton_Click;  // Add the event handler


            // Initialize the previous button
            previousButton = new Button();
            previousButton.Text = "Previous";
            previousButton.Location = new Point(fieldX - 50, startY + spacing * 13);
            previousButton.Size = new Size(100, 30);

            // Initialize the next button
            nextButton = new Button();
            nextButton.Text = "Next";
            nextButton.Location = new Point(fieldX + 60, startY + spacing * 13);
            nextButton.Size = new Size(100, 30);


            // Initialize the checkboxes
            //materialCheckBox = new CheckBox();
            initialCostCheckBox = new CheckBox();
            maintenanceCostCheckBox = new CheckBox();
            replacementCostCheckBox = new CheckBox();
            serviceLifeCheckBox = new CheckBox();
            residualValueCheckBox = new CheckBox();
            discountRateCheckBox = new CheckBox();
            otherCostsCheckBox = new CheckBox();
            //currencyCheckBox = new CheckBox();

            // Set the checkbox properties
            //materialCheckBox.Checked = true;
            initialCostCheckBox.Checked = true;
            maintenanceCostCheckBox.Checked = true;
            replacementCostCheckBox.Checked = true;
            serviceLifeCheckBox.Checked = true;
            residualValueCheckBox.Checked = true;
            discountRateCheckBox.Checked = true;
            otherCostsCheckBox.Checked = true;
            //currencyCheckBox.Checked = true;

            // Add event handlers for the checkboxes
            //materialCheckBox.CheckedChanged += (sender, e) => { materialField.Enabled = materialCheckBox.Checked; };
            initialCostCheckBox.CheckedChanged += (sender, e) => { initialCostField.Enabled = initialCostCheckBox.Checked; };
            maintenanceCostCheckBox.CheckedChanged += (sender, e) => { maintenanceCostField.Enabled = maintenanceCostCheckBox.Checked; };
            replacementCostCheckBox.CheckedChanged += (sender, e) => { replacementCostField.Enabled = replacementCostCheckBox.Checked; };
            serviceLifeCheckBox.CheckedChanged += (sender, e) => { serviceLifeField.Enabled = serviceLifeCheckBox.Checked; };
            residualValueCheckBox.CheckedChanged += (sender, e) => { residualValueField.Enabled = residualValueCheckBox.Checked; };
            discountRateCheckBox.CheckedChanged += (sender, e) => { discountRateField.Enabled = discountRateCheckBox.Checked; };
            otherCostsCheckBox.CheckedChanged += (sender, e) => { otherCostsField.Enabled = otherCostsCheckBox.Checked; };
            //currencyCheckBox.CheckedChanged += (sender, e) => { currencyField.Enabled = currencyCheckBox.Checked; };

            // Set the location and size for each checkbox
            int checkboxX = 5; // adjust this value as per your layout
                               //int checkboxX = 5;  // existing checkbox location


            //materialCheckBox.Location = new Point(checkboxX, startY);
            //materialCheckBox.Size = new Size(20, 20);

            initialCostCheckBox.Location = new Point(checkboxX, startY + spacing * 2);
            initialCostCheckBox.Size = new Size(20, 20);

            maintenanceCostCheckBox.Location = new Point(checkboxX, startY + spacing * 3);
            maintenanceCostCheckBox.Size = new Size(20, 20);

            replacementCostCheckBox.Location = new Point(checkboxX, startY + spacing * 4);
            replacementCostCheckBox.Size = new Size(20, 20);

            serviceLifeCheckBox.Location = new Point(checkboxX, startY + spacing * 5);
            serviceLifeCheckBox.Size = new Size(20, 20);

            otherCostsCheckBox.Location = new Point(checkboxX, startY + spacing * 6);
            otherCostsCheckBox.Size = new Size(20, 20);

            discountRateCheckBox.Location = new Point(checkboxX, startY + spacing * 7);
            discountRateCheckBox.Size = new Size(20, 20);

            residualValueCheckBox.Location = new Point(checkboxX, startY + spacing * 8);
            residualValueCheckBox.Size = new Size(20, 20);

            /*currencyCheckBox.Location = new Point(checkboxX, startY + spacing * 8);
            currencyCheckBox.Size = new Size(20, 20);*/

            // Add the controls to the form
            Controls.Add(materialLabel);
            Controls.Add(materialField);
            Controls.Add(locationLabel);
            Controls.Add(locationField);
            Controls.Add(initialCostLabel);
            Controls.Add(initialCostField);
            Controls.Add(maintenanceCostLabel);
            Controls.Add(maintenanceCostField);
            Controls.Add(replacementCostLabel);
            Controls.Add(replacementCostField);
            Controls.Add(serviceLifeLabel);
            Controls.Add(serviceLifeField);
            Controls.Add(otherCostsLabel);
            Controls.Add(otherCostsField);
            Controls.Add(discountRateLabel);
            Controls.Add(discountRateField);
            Controls.Add(residualValueLabel);      // newly added
            Controls.Add(residualValueField);      // newly added

            Controls.Add(currencyLabel);
            Controls.Add(currencyField);

            // Add the checkboxes to the form
            //Controls.Add(materialCheckBox);
            Controls.Add(initialCostCheckBox);
            Controls.Add(maintenanceCostCheckBox);
            Controls.Add(replacementCostCheckBox);
            Controls.Add(serviceLifeCheckBox);
            Controls.Add(otherCostsCheckBox);
            Controls.Add(discountRateCheckBox);
            Controls.Add(residualValueCheckBox);
            //Controls.Add(currencyCheckBox);

            Controls.Add(calculateButton);

            // Add these controls to the form
            Controls.Add(lccOutputLabel);
            Controls.Add(lccOutputField);
            Controls.Add(saveButton);

            // Add the buttons to the form
            Controls.Add(previousButton);
            Controls.Add(nextButton);  
    }

        public void SetMaterialField(string material)
        {
            materialField.Text = material;
        }
        public void SetInitialCostField(int value)
        {
            initialCostField.Text = value.ToString();
        }

        public void SetMaintenanceCostField(int value)
        {
            maintenanceCostField.Text = value.ToString();
        }

        public void SetReplacementCostField(int value)
        {
            replacementCostField.Text = value.ToString();
        }

        public void SetServiceLifeField(int value)
        {
            serviceLifeField.Text = value.ToString();
        }

        public void SetOtherCostsField(int value)
        {
            otherCostsField.Text = value.ToString();
        }

        public void SetDiscountRateField(int value)
        {
            discountRateField.Text = value.ToString();
        }

        public void SetResidualValueField(int value)
        {
            residualValueField.Text = value.ToString();
        }

        public void SetcurrencyField(string value)
        {
            currencyField.Text = value.ToString();//this might not be neccessary
        }


    private void CalculateButton_Click(object sender, EventArgs e)
    {
        // Get the values from the input fields
        int initialCost = int.Parse(initialCostField.Text);
        int maintenanceCost = int.Parse(maintenanceCostField.Text);
        int replacementCost = int.Parse(replacementCostField.Text);
        int serviceLife = int.Parse(serviceLifeField.Text);
        int otherCosts = int.Parse(otherCostsField.Text);
        int discountRate = int.Parse(discountRateField.Text);
        int residualValue = int.Parse(residualValueField.Text);

        // Perform the life cycle cost calculation
        int lifeCycleCost = CalculateLifeCycleCost(initialCost, maintenanceCost, replacementCost, serviceLife, otherCosts, discountRate, residualValue);
        string lccResult = lifeCycleCost.ToString() + "  " + currencyField.Text;
            DialogResult result = MessageBox.Show($"Life Cycle Cost: {lccResult}",
                                                   "Calculation Result", MessageBoxButtons.OKCancel);

            if (result == DialogResult.OK)
            {
                // User clicked OK button
                // Handle the OK button logic here
            }
            else if (result == DialogResult.Cancel)
            {
                // User clicked Cancel button
                // Handle the Cancel button logic here
            }

            //if (result == DialogResult.OK)
            //{
            //    // User clicked 'OK', save the result.
            //    SaveResult(lifeCycleCost);
            //}

            // Display the result in the output field as well as in a message box
            lccOutputField.Text = lifeCycleCost.ToString();
            //MessageBox.Show($"Life Cycle Cost: {lifeCycleCost}");

        }

    private int CalculateLifeCycleCost(int initialCost, int maintenanceCost, int replacementCost, int serviceLife, 
                                        int otherCosts, int discountRate, int residualValue)
    {
        int lifeCycleCost = initialCost;

        // Calculate the number of maintenance occurrences over the service life
        int maintenanceOccurrences = serviceLife / 12;

        // Multiply the maintenance cost by the number of maintenance occurrences
        lifeCycleCost += maintenanceCost * maintenanceOccurrences;

        lifeCycleCost += replacementCost;
        lifeCycleCost += otherCosts;
        lifeCycleCost -= residualValue;

        // Apply discount rate
        double discountFactor = Math.Pow(1 + (discountRate / 100.0), -serviceLife);
        lifeCycleCost = (int)Math.Round(lifeCycleCost * discountFactor);

        return lifeCycleCost;
    }


    /*private void SaveResult(int lifeCycleCost)
    {
        // Implement your save functionality here.
        // Here I will save the results at a data for shared parameter 
        // This was for the OK of the results popup. I will delete later
    }*/


    // Add the save button's Click event handler:
    private void SaveButton_Click(object sender, EventArgs e)
        {
            // Here you can save the results.
            // This is just an example of displaying a save dialog and saving the results to a file.

            // Optionally, you can display a message to indicate that the result has been saved
            // MessageBox.Show($"Saved result: {lifeCycleCost}");
        }

    }

