package com.ceb.reporting.phvobsoleteidle.model;

import com.fasterxml.jackson.annotation.JsonAlias;
import com.fasterxml.jackson.annotation.JsonProperty;

public class PhvObsoleteIdleRow {
    @JsonProperty("documentNo")
    @JsonAlias({"DOC_NO", "DocumentNo"})
    private String documentNo;

    @JsonProperty("materialCode")
    @JsonAlias({"MAT_CD", "MaterialCode"})
    private String materialCode;

    @JsonProperty("materialName")
    @JsonAlias({"MAT_NM", "MaterialName"})
    private String materialName;

    @JsonProperty("gradeCode")
    @JsonAlias({"GRADE_CD", "GradeCode"})
    private String gradeCode;

    @JsonProperty("phvDate")
    @JsonAlias({"PHV_DT", "PhvDate"})
    private String phvDate;

    @JsonProperty("qtyOnHand")
    @JsonAlias({"QTY_ON_HAND", "QtyOnHand"})
    private Double qtyOnHand;

    @JsonProperty("stockBook")
    @JsonAlias({"STOCKBOOK", "StockBook"})
    private Double stockBook;

    @JsonProperty("reason")
    @JsonAlias({"REASON", "Reason"})
    private String reason;

    @JsonProperty("costCentreName")
    @JsonAlias({"CCT_NAME", "CostCentreName"})
    private String costCentreName;

    public String getDocumentNo() {
        return documentNo;
    }

    public void setDocumentNo(String documentNo) {
        this.documentNo = documentNo;
    }

    public String getMaterialCode() {
        return materialCode;
    }

    public void setMaterialCode(String materialCode) {
        this.materialCode = materialCode;
    }

    public String getMaterialName() {
        return materialName;
    }

    public void setMaterialName(String materialName) {
        this.materialName = materialName;
    }

    public String getGradeCode() {
        return gradeCode;
    }

    public void setGradeCode(String gradeCode) {
        this.gradeCode = gradeCode;
    }

    public String getPhvDate() {
        return phvDate;
    }

    public void setPhvDate(String phvDate) {
        this.phvDate = phvDate;
    }

    public Double getQtyOnHand() {
        return qtyOnHand;
    }

    public void setQtyOnHand(Double qtyOnHand) {
        this.qtyOnHand = qtyOnHand;
    }

    public Double getStockBook() {
        return stockBook;
    }

    public void setStockBook(Double stockBook) {
        this.stockBook = stockBook;
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }

    public String getCostCentreName() {
        return costCentreName;
    }

    public void setCostCentreName(String costCentreName) {
        this.costCentreName = costCentreName;
    }

    // Uppercase getters for PhysicalVerification_Damage_FIFO_new.jrxml field names
    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getDOC_NO() {
        return documentNo;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getMAT_CD() {
        return materialCode;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getMAT_NM() {
        return materialName;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getGRADE_CD() {
        return gradeCode;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public java.sql.Timestamp getPHV_DT() {
        if (phvDate == null || phvDate.trim().isEmpty()) {
            return null;
        }
        try {
            java.util.Date parsed = new java.text.SimpleDateFormat("dd/MM/yyyy").parse(phvDate);
            return new java.sql.Timestamp(parsed.getTime());
        } catch (Exception e) {
            return null;
        }
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public java.math.BigDecimal getQTY_ON_HAND() {
        return qtyOnHand != null ? java.math.BigDecimal.valueOf(qtyOnHand) : null;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public java.math.BigDecimal getSTOCKBOOK() {
        return stockBook != null ? java.math.BigDecimal.valueOf(stockBook) : null;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getREASON() {
        return reason;
    }

    @com.fasterxml.jackson.annotation.JsonIgnore
    public String getCCT_NAME() {
        return costCentreName;
    }
}
