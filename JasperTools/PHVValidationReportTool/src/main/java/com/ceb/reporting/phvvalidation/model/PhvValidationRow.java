package com.ceb.reporting.phvvalidation.model;

import com.fasterxml.jackson.annotation.JsonProperty;

public class PhvValidationRow {
    @JsonProperty("MatCd")
    private String matCd;

    @JsonProperty("MatNm")
    private String matNm;

    @JsonProperty("UomCd")
    private String uomCd;

    @JsonProperty("GradeCd")
    private String gradeCd;

    @JsonProperty("QtyOnHand")
    private Double qtyOnHand;

    @JsonProperty("CntedQty")
    private Double cntedQty;

    @JsonProperty("UnitPrice")
    private Double unitPrice;

    @JsonProperty("Reason")
    private String reason;

    public String getMatCd() {
        return matCd;
    }

    public void setMatCd(String matCd) {
        this.matCd = matCd;
    }

    public String getMatNm() {
        return matNm;
    }

    public void setMatNm(String matNm) {
        this.matNm = matNm;
    }

    public String getUomCd() {
        return uomCd;
    }

    public void setUomCd(String uomCd) {
        this.uomCd = uomCd;
    }

    public String getGradeCd() {
        return gradeCd;
    }

    public void setGradeCd(String gradeCd) {
        this.gradeCd = gradeCd;
    }

    public Double getQtyOnHand() {
        return qtyOnHand;
    }

    public void setQtyOnHand(Double qtyOnHand) {
        this.qtyOnHand = qtyOnHand;
    }

    public Double getCntedQty() {
        return cntedQty;
    }

    public void setCntedQty(Double cntedQty) {
        this.cntedQty = cntedQty;
    }

    public Double getUnitPrice() {
        return unitPrice;
    }

    public void setUnitPrice(Double unitPrice) {
        this.unitPrice = unitPrice;
    }

    public String getReason() {
        return reason;
    }

    public void setReason(String reason) {
        this.reason = reason;
    }
}