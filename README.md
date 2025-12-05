<div align="center">
  <img src="Editor/Icons/icon.png" width="96" alt="Scriptable Object Tables Icon">
  <h1>Scriptable Object Tables</h1>
</div>

**Scriptable Object Tables** is a professional Unity editor tool designed to facilitate the management of `ScriptableObject` collections. It provides a streamlined, spreadsheet-like interface for viewing, creating, and modifying assets, significantly improving data management workflows for complex projects.

## Overview

Managing large numbers of ScriptableObjects through the default Inspector can be inefficient. This package addresses this challenge by aggregating assets into a customizable table view, allowing for rapid iteration and comparison of data.

### Key Features
- **Tabular Editing**: View and edit multiple assets simultaneously in a grid layout.
- **Type Safety**: Automatically restricts entries to specific `ScriptableObject` types to ensure data integrity.
- **Integrated Workflow**: Create new assets directly within the table interface without leaving the window.
- **Persistent Configuration**: Save table layouts and asset collections as project assets.

## Requirements

- Unity 2021.3 or later.
- **Dependencies**:
  - `com.solidalloy.type-references` (v2.15.1)

## Installation

This package can be installed directly via the Unity Package Manager using the Git URL.

1. Open your Unity project.
2. Navigate to **Window** > **Package Manager**.
3. Click the **+** (plus) icon in the top-left corner.
4. Select **Add package from git URL...**.
5. Enter the following URL:
   ```
   https://github.com/lucaswarwick02/scriptable-object-tables
   ```

## Usage

1. **Create a Table**: Right-click in the Project window and select **Create** > **Scriptable Object Tables** > **Scriptable Object Table**.
2. **Configure**: Select the target `ScriptableObject` type in the inspector.
3. **Edit**: Double-click the created asset to open the **Scriptable Object Table** editor window.
4. **Manage**: Use the toolbar to add new entries or save changes to the collection.
