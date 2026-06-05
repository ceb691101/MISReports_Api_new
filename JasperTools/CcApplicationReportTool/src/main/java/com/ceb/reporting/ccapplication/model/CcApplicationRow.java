package com.ceb.reporting.ccapplication.model;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonIgnore;
import com.fasterxml.jackson.annotation.JsonProperty;

import java.math.BigDecimal;
import java.sql.Timestamp;
import java.text.SimpleDateFormat;

public class CcApplicationRow {

    @JsonProperty("applicationId")
    @JsonAlias({"APPLICATION_ID", "ApplicationId"})
    private String applicationId;

    @JsonProperty("applicationNo")
    @JsonAlias({"APPLICATION_NO", "ApplicationNo"})
    private String applicationNo;

    @JsonProperty("pivReceiptNo")
    @JsonAlias({"PIV_RECEIPT_NO", "PivReceiptNo"})
    private String pivReceiptNo;

    @JsonProperty("pivNo")
    @JsonAlias({"PIV_NO", "PivNo"})
    private String pivNo;

    @JsonProperty("pivAmount")
    @JsonAlias({"PIV_AMOUNT", "PivAmount"})
    private Double pivAmount;

    @JsonProperty("name")
    @JsonAlias({"NAME", "Name"})
    private String name;

    @JsonProperty("applicationSubType")
    @JsonAlias({"APPLICATION_SUB_TYPE", "ApplicationSubType"})
    private String applicationSubType;

    @JsonProperty("piv1Date")
    @JsonAlias({"PIV1_DATE", "Piv1Date"})
    private String piv1Date;

    @JsonProperty("piv1No")
    @JsonAlias({"PIV1_NO", "Piv1No"})
    private String piv1No;

    @JsonProperty("piv1ReceiptNo")
    @JsonAlias({"PIV1_RECEIPT_NO", "Piv1ReceiptNo"})
    private String piv1ReceiptNo;

    @JsonProperty("streetAddress")
    @JsonAlias({"STREET_ADDRESS", "StreetAddress"})
    private String streetAddress;

    @JsonProperty("suburb")
    @JsonAlias({"SUBURB", "Suburb"})
    private String suburb;

    @JsonProperty("city")
    @JsonAlias({"CITY", "City"})
    private String city;

    @JsonProperty("paidDate")
    @JsonAlias({"PAID_DATE", "PaidDate"})
    private String paidDate;

    @JsonProperty("tariffCatCode")
    @JsonAlias({"TARIFF_CAT_CODE", "TariffCatCode"})
    private String tariffCatCode;

    @JsonProperty("phase")
    @JsonAlias({"PHASE", "Phase"})
    private Double phase;

    @JsonProperty("connectionType")
    @JsonAlias({"CONNECTION_TYPE", "ConnectionType"})
    private Double connectionType;

    @JsonProperty("projectNo")
    @JsonAlias({"PROJECTNO", "ProjectNo"})
    private String projectNo;

    @JsonProperty("accNo")
    @JsonAlias({"ACC_NO", "AccNo"})
    private String accNo;

    @JsonProperty("cctName")
    @JsonAlias({"CCT_NAME", "CctName"})
    private String cctName;

    // ───────────────────────────────────────────────────────────
    // Standard getters / setters
    // ───────────────────────────────────────────────────────────

    public String getApplicationId() { return applicationId; }
    public void setApplicationId(String applicationId) { this.applicationId = applicationId; }

    public String getApplicationNo() { return applicationNo; }
    public void setApplicationNo(String applicationNo) { this.applicationNo = applicationNo; }

    public String getPivReceiptNo() { return pivReceiptNo; }
    public void setPivReceiptNo(String pivReceiptNo) { this.pivReceiptNo = pivReceiptNo; }

    public String getPivNo() { return pivNo; }
    public void setPivNo(String pivNo) { this.pivNo = pivNo; }

    public Double getPivAmount() { return pivAmount; }
    public void setPivAmount(Double pivAmount) { this.pivAmount = pivAmount; }

    public String getName() { return name; }
    public void setName(String name) { this.name = name; }

    public String getApplicationSubType() { return applicationSubType; }
    public void setApplicationSubType(String applicationSubType) { this.applicationSubType = applicationSubType; }

    public String getPiv1Date() { return piv1Date; }
    public void setPiv1Date(String piv1Date) { this.piv1Date = piv1Date; }

    public String getPiv1No() { return piv1No; }
    public void setPiv1No(String piv1No) { this.piv1No = piv1No; }

    public String getPiv1ReceiptNo() { return piv1ReceiptNo; }
    public void setPiv1ReceiptNo(String piv1ReceiptNo) { this.piv1ReceiptNo = piv1ReceiptNo; }

    public String getStreetAddress() { return streetAddress; }
    public void setStreetAddress(String streetAddress) { this.streetAddress = streetAddress; }

    public String getSuburb() { return suburb; }
    public void setSuburb(String suburb) { this.suburb = suburb; }

    public String getCity() { return city; }
    public void setCity(String city) { this.city = city; }

    public String getPaidDate() { return paidDate; }
    public void setPaidDate(String paidDate) { this.paidDate = paidDate; }

    public String getTariffCatCode() { return tariffCatCode; }
    public void setTariffCatCode(String tariffCatCode) { this.tariffCatCode = tariffCatCode; }

    public Double getPhase() { return phase; }
    public void setPhase(Double phase) { this.phase = phase; }

    public Double getConnectionType() { return connectionType; }
    public void setConnectionType(Double connectionType) { this.connectionType = connectionType; }

    public String getProjectNo() { return projectNo; }
    public void setProjectNo(String projectNo) { this.projectNo = projectNo; }

    public String getAccNo() { return accNo; }
    public void setAccNo(String accNo) { this.accNo = accNo; }

    public String getCctName() { return cctName; }
    public void setCctName(String cctName) { this.cctName = cctName; }

    // ───────────────────────────────────────────────────────────
    // Uppercase getters for .jrxml field resolution via bean
    // introspection. The template expects fields like
    // APPLICATION_ID (String), PIV_AMOUNT (BigDecimal),
    // PAID_DATE (Timestamp), PHASE (BigDecimal), etc.
    // ───────────────────────────────────────────────────────────

    @JsonIgnore
    public String getAPPLICATION_ID() { return applicationId; }

    @JsonIgnore
    public String getAPPLICATION_NO() { return applicationNo; }

    @JsonIgnore
    public String getPIV_RECEIPT_NO() { return pivReceiptNo; }

    @JsonIgnore
    public String getPIV_NO() { return pivNo; }

    @JsonIgnore
    public BigDecimal getPIV_AMOUNT() {
        return pivAmount != null ? BigDecimal.valueOf(pivAmount) : null;
    }

    @JsonIgnore
    public String getNAME() { return name; }

    @JsonIgnore
    public String getAPPLICATION_SUB_TYPE() { return applicationSubType; }

    @JsonIgnore
    public Timestamp getPIV1_DATE() {
        return parseTimestamp(piv1Date);
    }

    @JsonIgnore
    public String getPIV1_NO() { return piv1No; }

    @JsonIgnore
    public String getPIV1_RECEIPT_NO() { return piv1ReceiptNo; }

    @JsonIgnore
    public String getSTREET_ADDRESS() { return streetAddress; }

    @JsonIgnore
    public String getSUBURB() { return suburb; }

    @JsonIgnore
    public String getCITY() { return city; }

    @JsonIgnore
    public Timestamp getPAID_DATE() {
        return parseTimestamp(paidDate);
    }

    @JsonIgnore
    public String getTARIFF_CAT_CODE() { return tariffCatCode; }

    @JsonIgnore
    public BigDecimal getPHASE() {
        return phase != null ? BigDecimal.valueOf(phase) : null;
    }

    @JsonIgnore
    public BigDecimal getCONNECTION_TYPE() {
        return connectionType != null ? BigDecimal.valueOf(connectionType) : null;
    }

    @JsonIgnore
    public String getPROJECTNO() { return projectNo; }

    @JsonIgnore
    public String getACC_NO() { return accNo; }

    @JsonIgnore
    public String getCCT_NAME() { return cctName; }

    // ───────────────────────────────────────────────────────────
    // Helper to parse ISO date strings into java.sql.Timestamp
    // ───────────────────────────────────────────────────────────

    private static Timestamp parseTimestamp(String value) {
        if (value == null || value.trim().isEmpty()) {
            return null;
        }
        try {
            // Handle dd/MM/yyyy format (after service-layer formatting)
            if (value.contains("/") && !value.contains("T")) {
                java.util.Date parsed = new SimpleDateFormat("dd/MM/yyyy").parse(value);
                return new Timestamp(parsed.getTime());
            }
            // Handle ISO format yyyy-MM-dd'T'HH:mm:ss
            String isoTrimmed = value.length() > 19 ? value.substring(0, 19) : value;
            java.util.Date parsed = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss").parse(isoTrimmed);
            return new Timestamp(parsed.getTime());
        } catch (Exception e) {
            return null;
        }
    }
}
