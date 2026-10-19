# RevitWoodLCC

RevitWoodLCC is an Autodesk Revit add-in that integrates service-life estimation for wood construction elements into Building Information Modelling (BIM) workflows. It estimates the onset of fungal decay using material resistance and environmental exposure information and stores results against BIM elements.

The software was developed as part of the WoodLCC project (Life Cycle Costing: Optimizing the Lifetime Performance of Timber Buildings), funded through the ForestValue programme.

## Key features

- Element-level service-life estimation for wood construction elements.
- Material and treatment selection using external JSON data.
- Consideration of environmental exposure and construction-detail factors.
- Storage of service-life results as Revit element parameters.
- 3D visualization of predicted service-life results in the BIM model.
- Custom IFC property sets to support exchange of wood-treatment and service-life information.

## Supported environment

The current documented target environment is:

- **Operating system:** Windows
- **BIM application:** Autodesk Revit 2023
- **Development language:** C#
- **UI technology:** Windows Presentation Foundation (WPF)
- **Revit dependencies:** `RevitAPI.dll` and `RevitAPIUI.dll`
- **Data files:** JSON material and environmental datasets

Check the project file and the release documentation for the exact .NET target framework and other dependencies of the release you are using. Compatibility with Revit versions other than Revit 2023 has not been verified.

## Versioned archive and source provenance

The v2 software archive is preserved on Zenodo:

- **Archived version:** v2
- **Zenodo record:** [https://zenodo.org/records/17279343](https://zenodo.org/records/17279343)
- **DOI:** [10.5281/zenodo.17279343](https://doi.org/10.5281/zenodo.17279343)
- **Source commit recorded in the supplied archive:** `518defcb56a2c9368bb6b6ef9daac578bbea8cf3` (4 March 2025)

The uploaded Zenodo v2 archive contains Git metadata identifying the source commit above, and that same commit is present in this GitHub repository's history. This links the archived source snapshot to a specific repository commit. The Zenodo record date identifies when the archive was published; it should not be interpreted as the date of the source commit.

For reproducibility, a GitHub release/tag should point to the source commit that matches the archived version. A GitHub release has not yet been created; until it is published, use the Zenodo record above as the permanent archive for v2.

## Installation

### Option A — Install a packaged release

If a release package is provided on the repository's **Releases** page, use that package rather than downloading individual source files.

1. Confirm that the release identifies the Revit version it supports. The current documented target is Revit 2023 on Windows.
2. Download the release package and extract it to a local folder.
3. Close Autodesk Revit before installing the add-in.
4. Follow the release-specific installation notes. The package should identify the add-in assembly (DLL), the required data files and any Revit add-in manifest (`.addin`) needed for registration.
5. If the package does not include an installer, copy the files to the locations specified in the release instructions and ensure that the `.addin` manifest points to the correct assembly path and the correct entry-point class. Do not use a manifest from another version unless it has been confirmed to match the build.
6. Start the supported version of Revit and check the relevant ribbon tab or command location described in the release notes.
7. Open a test project and run a calculation on a non-critical model element before using the add-in in a production project.

**Important:** Do not copy only the DLL if the release also requires JSON datasets, other libraries or a `.addin` manifest. Keep the required files together as described in the release package.

### Option B — Build from source

Building from source is intended for developers familiar with Visual Studio and the Revit API.

1. Install Visual Studio with the .NET development components required by the project.
2. Install the target version of Autodesk Revit.
3. Clone or download this repository and open the solution or project file in Visual Studio.
4. Inspect the project properties to confirm the target framework and configuration used by the release.
5. Check the references to `RevitAPI.dll` and `RevitAPIUI.dll`. They must point to the API assemblies for the Revit version you are building against.
6. Restore any NuGet or other project dependencies, if applicable, and build the solution.
7. Resolve build errors before attempting to load the add-in in Revit.
8. Deploy the compiled output, required JSON datasets and the correct `.addin` manifest according to the project's deployment configuration. The manifest must reference the actual assembly path and entry-point class used by the build.
9. Launch the target Revit version and test the add-in in a separate test model.

The exact project configuration and deployment files in the repository take precedence over these general instructions. If a prebuilt package or a version-specific installation guide is supplied with a release, follow that documentation.

## Using the add-in

The general workflow is:

1. Open a Revit project and select the element or elements to assess.
2. Define the project location.
3. Select the wood material and treatment.
4. Specify the applicable exposure and construction-detail conditions.
5. Calculate the predicted service-life duration.
6. Save the results to the BIM model and inspect the visualization.

The available inputs and commands depend on the installed release. The predictions are estimates of fungal-decay onset and should not be interpreted as predictions of structural failure or a substitute for professional assessment.

## Compatibility with other Revit versions

Compatibility with other Autodesk Revit versions is **not automatic**. RevitWoodLCC depends on the Revit API assemblies and the .NET runtime supported by the target Revit release. To adapt the source code to another version, a developer should:

1. Install the target Revit version and its supported development/runtime components.
2. Update the project target framework as required by that Revit version.
3. Update the `RevitAPI.dll` and `RevitAPIUI.dll` references to the assemblies for the target version.
4. Review and adjust API calls, build settings and dependencies where required.
5. Rebuild the project and resolve compilation errors.
6. Update the add-in manifest and deployment package for the new build.
7. Test the add-in's installation, commands, calculations, data loading, parameter storage and visualization in the target Revit version.

Revit 2025 introduced a transition from .NET Framework to .NET 8 for Revit add-ins. Adapting a build targeting Revit 2023 to Revit 2025 or later therefore requires careful review of the target framework and dependencies, and may require code changes. These instructions describe a general adaptation process; they do not indicate that other versions have been tested or are officially supported. A separate build should only be described as supported after it has been tested.

## Operating-system support

RevitWoodLCC uses Autodesk Revit and Windows Presentation Foundation (WPF). The current software is intended for a compatible Windows environment running Autodesk Revit and does not provide native macOS support. Running Windows and Revit through a virtualized environment on an Apple computer is a separate configuration and has not been validated for this add-in.

## Data and limitations

Material and environmental information is stored in external JSON data files. Keep the data files required by the release available at the locations expected by the application. Do not change the file structure or property names unless you understand how the software reads them.

The predictions are risk-oriented estimates of the onset of fungal decay. They do not directly model all moisture, thermal or solar-radiation processes and should be interpreted alongside professional judgement and condition monitoring.

## Troubleshooting

- **The add-in does not appear in Revit:** Confirm that the `.addin` manifest is in the correct Revit add-ins directory, points to the correct DLL, and identifies the correct entry-point class.
- **Revit cannot load the add-in:** Confirm that the DLL was built for the intended Revit/.NET environment and that all required dependencies are available.
- **Data cannot be loaded:** Confirm that the JSON files are included and located where the application expects them.
- **The project does not compile:** Check the target framework, Revit API references and project dependencies for the intended Revit version.
- **A newer Revit version is being used:** Do not assume the existing build will work. Follow the compatibility steps above and test a version-specific build.

For questions or to report an issue, contact **richard.acquah@innorenew.eu**.

## Licence

This software is distributed under the MIT License. See the [LICENSE](LICENSE) file for the terms and conditions.

## Acknowledgements

This work was developed within the WoodLCC project, supported by the ForestValue Research Programme and the European Union's Horizon 2020 research and innovation programme (Grant Agreement No. 773324).
