package com.ceb.reporting.solarpending.model;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.sql.Timestamp;
import java.text.SimpleDateFormat;

public class SolarPendingJobsRow {

    @JsonProperty("applicationId")
    @JsonAlias({"APPLICATION_ID", "ApplicationId"})
    private String applicationId;

    @JsonProperty("applicationNo")
    @JsonAlias({"APPLICATION_NO", "ApplicationNo"})
    private String applicationNo;

    @JsonProperty("submitDate")
    @JsonAlias({"SUBMIT_DATE", "SubmitDate"})
    private String submitDate;

    @JsonProperty("projectNo")
    @JsonAlias({"PROJECTNO", "ProjectNo"})
    private String projectNo;

    @JsonProperty("pivDate")
    @JsonAlias({"PIV_DATE", "PivDate"})
    private String pivDate;

    @JsonProperty("applicationSubType")
    @JsonAlias({"APPLICATION_SUB_TYPE", "ApplicationSubType"})
    private String applicationSubType;

    @JsonProperty("paidDate")
    @JsonAlias({"PAID_DATE", "PaidDate"})
    private String paidDate;

    @JsonProperty("piv2PaidDate")
    @JsonAlias({"PIV2_PAID_DATE", "Piv2PaidDate"})
    private String piv2PaidDate;

    @JsonProperty("existingAccNo")
    @JsonAlias({"EXISTING_ACC_NO", "ExistingAccNo"})
    private String existingAccNo;

    @JsonProperty("status")
    @JsonAlias({"STATUS", "Status"})
    private String status;

    @JsonProperty("deptId")
    @JsonAlias({"DEPT_ID", "DeptId"})
    private String deptId;

    @JsonProperty("cctName")
    @JsonAlias({"CCT_NAME", "CctName"})
    private String cctName;

    @JsonProperty("provinceName")
    @JsonAlias({"PROVINCE_NAME", "ProvinceName"})
    private String provinceName;

    // Getters / Setters
    public String getApplicationId() { return applicationId; }
    public void setApplicationId(String applicationId) { this.applicationId = applicationId; }

    public String getApplicationNo() { return applicationNo; }
    public void setApplicationNo(String applicationNo) { this.applicationNo = applicationNo; }

    public String getSubmitDate() { return submitDate; }
    public void setSubmitDate(String submitDate) { this.submitDate = submitDate; }

    public String getProjectNo() { return projectNo; }
    public void setProjectNo(String projectNo) { this.projectNo = projectNo; }

    public String getPivDate() { return pivDate; }
    public void setPivDate(String pivDate) { this.pivDate = pivDate; }

    public String getApplicationSubType() { return applicationSubType; }
    public void setApplicationSubType(String applicationSubType) { this.applicationSubType = applicationSubType; }

    public String getPaidDate() { return paidDate; }
    public void setPaidDate(String paidDate) { this.paidDate = paidDate; }

    public String getPiv2PaidDate() { return piv2PaidDate; }
    public void setPiv2PaidDate(String piv2PaidDate) { this.piv2PaidDate = piv2PaidDate; }

    public String getExistingAccNo() { return existingAccNo; }
    public void setExistingAccNo(String existingAccNo) { this.existingAccNo = existingAccNo; }

    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }

    public String getDeptId() { return deptId; }
    public void setDeptId(String deptId) { this.deptId = deptId; }

    public String getCctName() { return cctName; }
    public void setCctName(String cctName) { this.cctName = cctName; }

    public String getProvinceName() { return provinceName; }
    public void setProvinceName(String provinceName) { this.provinceName = provinceName; }

    // Uppercase getters for JRXML fields
    @JsonIgnore
    public String getDEPT_ID() { return deptId; }

    @JsonIgnore
    public String getAPPLICATION_ID() { return applicationId; }

    @JsonIgnore
    public String getAPPLICATION_NO() { return applicationNo; }

    @JsonIgnore
    public Timestamp getSUBMIT_DATE() { return parseTimestamp(submitDate); }

    @JsonIgnore
    public String getPROJECTNO() { return projectNo; }

    @JsonIgnore
    public Timestamp getPIV_DATE() { return parseTimestamp(pivDate); }

    @JsonIgnore
    public String getAPPLICATION_SUB_TYPE() { return applicationSubType; }

    @JsonIgnore
    public Timestamp getPAID_DATE() { return parseTimestamp(paidDate); }

    @JsonIgnore
    public Timestamp getPIV2_PAID_DATE() { return parseTimestamp(piv2PaidDate); }

    @JsonIgnore
    public String getEXISTING_ACC_NO() { return existingAccNo; }

    @JsonIgnore
    public String getSTATUS() { return status; }

    @JsonIgnore
    public String getCOMP_NM() { return provinceName; }

    private static Timestamp parseTimestamp(String value) {
        if (value == null || value.trim().isEmpty()) {
            return null;
        }
        try {
            if (value.contains("/") && !value.contains("T")) {
                java.util.Date parsed = new SimpleDateFormat("dd/MM/yyyy").parse(value);
                return new Timestamp(parsed.getTime());
            }
            String isoTrimmed = value.length() > 19 ? value.substring(0, 19) : value;
            java.util.Date parsed = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss").parse(isoTrimmed);
            return new Timestamp(parsed.getTime());
        } catch (Exception e) {
            return null;
        }
    }
}
