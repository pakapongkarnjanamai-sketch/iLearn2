# Technical Guide for Developing a SCORM Player (1.2 & 2004)

This comprehensive guide outlines the technical requirements for building a Learning Management System (LMS) component capable of reading, launching, and tracking both SCORM 1.2 and SCORM 2004 content.

SCORM is composed of sub-specifications that you must implement to achieve conformance. SCORM 1.2 focuses on two main parts: the Run-Time Environment and the Content Aggregation Model. SCORM 2004 introduces a third part: the Sequencing and Navigation specification.

---

## 1. Content Aggregation Model & Packaging

The Content Aggregation Model specifies how content should be packaged so that your LMS can import it. 

### Package Structure
* **The PIF (Package Interchange File):** All content must be delivered in a self-contained directory or a ZIP file.
* **The Manifest File:** The package must always contain an XML file named `imsmanifest.xml` located strictly at the root of the ZIP file.
* **Data Extraction:** Your system must parse `imsmanifest.xml` because it contains all the information the LMS needs to deliver the content.

### Content Structure (SCOs and Assets)
The manifest divides the course into logical units of learning:
* **Assets:** Electronic representations of media, text, images, sound, web pages, or assessment objects.
* **SCOs (Sharable Content Objects):** A collection of one or more assets that represents the smallest unit of learning that the LMS should track. 
* **Activity Tree:** SCOs can be combined into a nested tree structure that represents the course.

### Metadata (Optional but Recommended)
The manifest can contain metadata that describes the course using a pre-defined vocabulary divided into nine categories: General, Lifecycle, Meta-metadata, Technical, Educational, Rights, Relation, Annotation, and Classification. This is completely optional in SCORM 2004.

---

## 2. Run-Time Environment (RTE) & The API Adapter

The Run-Time Environment specifies how content should behave once launched by the LMS. All content is launched in a web browser, either in a new window or in a frameset.

To support both versions, your LMS must provide an ECMAScript (JavaScript) API Adapter. 

### API Discovery & Naming
The content uses a well-defined algorithm to locate the API provided by the LMS. The API Adapter must reside in a window that is an opener window or a parent frame of the window containing the content.

To support both SCORM 1.2 and SCORM 2004 simultaneously, your application must expose two distinct JavaScript objects in the DOM:
* **For SCORM 1.2:** The object must be named `API`.
* **For SCORM 2004:** The object must be named `API_1484_11`.

### API Functions
The API provides functions that permit the exchange of data with the LMS. Both versions require exactly 8 functions, but their names differ:

| Action | SCORM 1.2 (`API`) | SCORM 2004 (`API_1484_11`) | Description |
| :--- | :--- | :--- | :--- |
| **Initialize** | `LMSInitialize("")` | `Initialize("")` | Called when the content starts. |
| **Terminate** | `LMSFinish("")` | `Terminate("")` | Called when the content exits. |
| **Get Data** | `LMSGetValue(element)` | `GetValue(element)` | Retrieves a data model element's value from the LMS. |
| **Set Data** | `LMSSetValue(element, value)`| `SetValue(element, value)` | Writes a value for a data model element to the LMS. |
| **Save/Persist**| `LMSCommit("")` | `Commit("")` | Called to ensure that the data is persisted to the server. |
| **Error Code** | `LMSGetLastError()` | `GetLastError()` | Retrieves the last error code. |
| **Error String**| `LMSGetErrorString(code)`| `GetErrorString(code)` | Retrieves a text description of the error code. |
| **Diagnostic** | `LMSGetDiagnostic(code)` | `GetDiagnostic(code)` | Retrieves detailed diagnostic information about the error. |

### The CMI Data Model
The CMI data model provides a vocabulary of data elements that can be written to and read from the LMS (e.g., status, score, bookmark, time spent). 

*Note: The vocabulary differs between versions.*
* **Bookmark Example:** SCORM 1.2 uses `cmi.core.lesson_location` while SCORM 2004 uses `cmi.location`.

---

## 3. Sequencing and Navigation (SCORM 2004 Only)

If you are fully supporting SCORM 2004, you must parse and execute Sequencing rules. This specification allows the content author to govern how the learner navigates between SCOs and how progress data is rolled up.

* **XML Rules:** Sequencing rules are represented by XML within the course's manifest (`imsmanifest.xml`).
* **Tracking Model:** Sequencing operates on a tracking model that closely parallels the CMI data reported by SCOs during run-time.
* **Rule Categories:** Authors can define rules such as:
    * **Sequencing Control Modes:** Determine allowed navigation (e.g., table of contents vs. previous/next buttons).
    * **Limit Conditions:** Provide limits on the number of attempts.
    * **Rollup Rules:** Determine how status is rolled up through the hierarchy (e.g., if one child is satisfied, mark the module as completed).
    * **Randomization & Selection:** Randomly select a subset of available SCOs or shuffle their order.
    * **Prerequisites:** Specify that certain activities must be completed before others.

---

## 4. Implementation Concept

To make your LMS conformant, you will need to handle the browser-to-server communication, ensuring the API adapters run locally in the user's browser, while permanently persisting data to your backend (e.g., using AJAX) during `Commit()` calls.

```javascript
// A conceptual wrapper to inject into the parent frame/window

// 1. SCORM 1.2 API
window.API = {
    LMSInitialize: function(param) { /* Init logic */ return "true"; },
    LMSFinish: function(param) { /* Finish logic */ return "true"; },
    LMSGetValue: function(element) { /* Return 1.2 specific data */ return ""; },
    LMSSetValue: function(element, value) { /* Save 1.2 specific data */ return "true"; },
    LMSCommit: function(param) { /* AJAX call to backend */ return "true"; },
    LMSGetLastError: function() { return "0"; },
    LMSGetErrorString: function(errorCode) { return "No error"; },
    LMSGetDiagnostic: function(errorCode) { return ""; }
};

// 2. SCORM 2004 API
window.API_1484_11 = {
    Initialize: function(param) { /* Init logic */ return "true"; },
    Terminate: function(param) { /* Finish logic */ return "true"; },
    GetValue: function(element) { /* Return 2004 specific data */ return ""; },
    SetValue: function(element, value) { /* Save 2004 specific data */ return "true"; },
    Commit: function(param) { /* AJAX call to backend */ return "true"; },
    GetLastError: function() { return "0"; },
    GetErrorString: function(errorCode) { return "No error"; },
    GetDiagnostic: function(errorCode) { return ""; }
};