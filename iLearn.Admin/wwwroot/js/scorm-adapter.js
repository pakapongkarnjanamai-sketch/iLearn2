var ScormAdapter = (function () {
    // State ภายในสำหรับเก็บข้อมูล (Unified Data Model)
    var _cmi = {
        status: "not attempted",
        score_raw: 0,
        score_min: 0,
        score_max: 0,
        location: "",
        suspend_data: "",
        session_time: "00:00:00",
        total_time: "00:00:00"
    };

    var _isInitialized = false;
    var _settings = {};
    var _version = "1.2"; // "1.2" or "2004"

    // --- Helper Functions ---

    // แปลงเวลา SCORM 2004 (ISO8601: PT1H30M) เป็น SCORM 1.2 (HH:MM:SS) เพื่อเก็บใน DB เดิมได้
    function convertIsoToTimeSpan(isoStr) {
        // (Implementation แบบย่อ: ใช้ Regex ดึงตัวเลข)
        // เพื่อความง่าย ใน Production ควรใช้ Library เช่น moment.js หรือเขียน Parser เต็มรูปแบบ
        // อันนี้เป็น Dummy logic เพื่อไม่ให้ error
        if (!isoStr || isoStr.indexOf("P") !== 0) return isoStr;
        return "00:00:00"; // Placeholder: ต้องเขียน Logic แปลงจริงถ้าจำเป็น
    }

    // รวมสถานะ 2004 (Completion + Success) เป็น Status เดียวเหมือน 1.2
    function deriveStatus2004() {
        var completion = _cmi.completion_status || "unknown";
        var success = _cmi.success_status || "unknown";

        if (success === "passed") return "passed";
        if (success === "failed") return "failed";
        if (completion === "completed") return "completed";
        return "incomplete";
    }

    // --- Core Logic ---

    function initialize(version) {
        console.log("[LMS] Initialize " + version);
        _version = version;
        var result = "false";

        $.ajax({
            url: _settings.serviceUrl + '/scorm/initialize',
            type: 'GET',
            async: false,
            xhrFields: { withCredentials: true },
            data: {
                courseVersionId: _settings.courseVersionId,
                resourceId: _settings.resourceId
            },
            success: function (res) {
                if (res.success) {
                    var d = res.Data || res.data;
                    // Map ข้อมูลจาก DB ลง State
                    _cmi.status = d.cmi_core_lesson_status;
                    _cmi.location = d.cmi_core_lesson_location;
                    _cmi.suspend_data = d.cmi_suspend_data;
                    _cmi.score_raw = d.cmi_core_score_raw;
                    _cmi.total_time = d.cmi_core_total_time;

                    // 2004 Specific Init
                    _cmi.completion_status = (_cmi.status === 'passed' || _cmi.status === 'completed') ? 'completed' : 'incomplete';
                    _cmi.success_status = (_cmi.status === 'passed') ? 'passed' : 'unknown';

                    _isInitialized = true;
                    result = "true";
                }
            }
        });
        return result;
    }

    function commit() {
        if (!_isInitialized) return "false";
        console.log("[LMS] Commit data...");

        // เตรียม Payload (ใช้ DTO เดิมของ Backend ได้เลย)
        var payload = {
            CourseVersionId: _settings.courseVersionId,
            ResourceId: _settings.resourceId,
            LessonStatus: _version === "2004" ? deriveStatus2004() : _cmi.status,
            LessonLocation: _cmi.location,
            SuspendData: _cmi.suspend_data,
            ScoreRaw: parseFloat(_cmi.score_raw) || 0,
            SessionTime: _cmi.session_time
        };

        var result = "false";
        $.ajax({
            url: _settings.serviceUrl + '/scorm/commit',
            type: 'POST',
            xhrFields: { withCredentials: true },
            async: false, // SCORM บังคับ Sync
            contentType: "application/json",
            data: JSON.stringify(payload),
            success: function (res) {
                if (res.success) result = "true";
            }
        });
        return result;
    }

    // --- Public Interfaces ---

    return {
        initAdapter: function (settings) {
            _settings = settings;
        },

        // === SCORM 1.2 API ===
        API_12: {
            LMSInitialize: function (p) { return initialize("1.2"); },
            LMSFinish: function (p) { return commit(); /* and terminate */ },
            LMSGetValue: function (element) {
                // Map SCORM 1.2 element names to internal _cmi state
                switch (element) {
                    case "cmi.core.lesson_status": return _cmi.status;
                    case "cmi.core.lesson_location": return _cmi.location;
                    case "cmi.suspend_data": return _cmi.suspend_data;
                    case "cmi.core.score.raw": return _cmi.score_raw;
                    case "cmi.core.total_time": return _cmi.total_time;
                    case "cmi.core.student_id": return _settings.studentCode;
                    case "cmi.core.student_name": return "Student";
                }
                return "";
            },
            LMSSetValue: function (element, value) {
                // Map 1.2 keys to internal state
                switch (element) {
                    case "cmi.core.lesson_status": _cmi.status = value; break;
                    case "cmi.core.lesson_location": _cmi.location = value; break;
                    case "cmi.suspend_data": _cmi.suspend_data = value; break;
                    case "cmi.core.score.raw": _cmi.score_raw = value; break;
                    case "cmi.core.session_time": _cmi.session_time = value; break;
                }
                return "true";
            },
            LMSCommit: function (p) { return commit(); },
            LMSGetLastError: function () { return "0"; },
            LMSGetErrorString: function () { return ""; },
            LMSGetDiagnostic: function () { return ""; }
        },

        // === SCORM 2004 API (API_1484_11) ===
        API_2004: {
            Initialize: function (p) { return initialize("2004"); },
            Terminate: function (p) { return commit(); },
            GetValue: function (element) {
                // Map SCORM 2004 element names
                switch (element) {
                    case "cmi.completion_status": return _cmi.completion_status;
                    case "cmi.success_status": return _cmi.success_status;
                    case "cmi.location": return _cmi.location; // ไม่มี core.lesson_
                    case "cmi.suspend_data": return _cmi.suspend_data;
                    case "cmi.score.raw": return _cmi.score_raw;
                    case "cmi.learner_id": return _settings.studentCode;
                    case "cmi.learner_name": return "Student";
                }
                return "";
            },
            SetValue: function (element, value) {
                // Map 2004 keys
                switch (element) {
                    case "cmi.completion_status": _cmi.completion_status = value; break;
                    case "cmi.success_status": _cmi.success_status = value; break;
                    case "cmi.location": _cmi.location = value; break;
                    case "cmi.suspend_data": _cmi.suspend_data = value; break;
                    case "cmi.score.raw": _cmi.score_raw = value; break;
                    case "cmi.session_time":
                        // 2004 ส่งมาเป็น PT1H30M (ISO) เราอาจต้องแปลงเป็น 01:30:00 เก็บไว้
                        // สำหรับตัวอย่างนี้ขอเก็บไปก่อน แล้วค่อยไปแก้ Backend ให้รองรับ String ยาวๆ
                        _cmi.session_time = value;
                        break;
                }
                return "true";
            },
            Commit: function (p) { return commit(); },
            GetLastError: function () { return "0"; },
            GetErrorString: function () { return ""; },
            GetDiagnostic: function () { return ""; }
        }
    };
})();

// Expose Interfaces for Content to Find
window.API = ScormAdapter.API_12;          // For SCORM 1.2
window.API_1484_11 = ScormAdapter.API_2004; // For SCORM 2004