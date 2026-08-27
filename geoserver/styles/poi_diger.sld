<?xml version="1.0" encoding="UTF-8"?>
<!--
  Kendi stili tanimlanmamis kategoriler: gri ucgen.

  Kategoriler yoneticinin arayuzden ekleyebildigi dinamik bir agac; her
  yeni kategoriye elle SLD yazmak gerekmesin diye bu yedek var. Filtre
  "bilinen kategorilerin HICBIRI degil" diyor, dolayisiyla yeni bir
  kategori eklendiginde POI'leri kaybolmuyor, gri ucgen olarak cikiyor.

  Yeni bir kategoriye kendi gorunumu verilecekse: buna benzer bir SLD
  yazilip GeoServer'a yuklenmeli, adi appsettings icindeki PoiStyles
  listesine eklenmeli ve buradaki "bilinenler" listesine de girmeli.
-->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>poi_diger</Name>
    <UserStyle>
      <Title>POI - Diğer</Title>
      <FeatureTypeStyle>
        <Rule>
          <Name>isaret</Name>
          <ogc:Filter>
          <ogc:And>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Yeme-İçme*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Konaklama*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Sağlık*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
          </ogc:And>
          </ogc:Filter>
          <PointSymbolizer>
            <Graphic>
              <Mark>
                <WellKnownName>triangle</WellKnownName>
                <Fill><CssParameter name="fill">#64748b</CssParameter></Fill>
                <Stroke>
                  <CssParameter name="stroke">#ffffff</CssParameter>
                  <CssParameter name="stroke-width">2</CssParameter>
                </Stroke>
              </Mark>
              <Size>14</Size>
            </Graphic>
          </PointSymbolizer>
        </Rule>
        <Rule>
          <Name>isim-etiketi</Name>
          <ogc:Filter>
          <ogc:And>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Yeme-İçme*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Konaklama*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
            <ogc:Not>
          <ogc:PropertyIsLike wildCard="*" singleChar="." escapeChar="!">
            <ogc:PropertyName>kategori_yolu</ogc:PropertyName>
            <ogc:Literal>Sağlık*</ogc:Literal>
          </ogc:PropertyIsLike>
            </ogc:Not>
          </ogc:And>
          </ogc:Filter>
          <MaxScaleDenominator>2000000</MaxScaleDenominator>
          <TextSymbolizer>
            <Label><ogc:PropertyName>isim</ogc:PropertyName></Label>
            <Font>
              <CssParameter name="font-family">SansSerif</CssParameter>
              <CssParameter name="font-size">12</CssParameter>
              <CssParameter name="font-weight">bold</CssParameter>
            </Font>
            <LabelPlacement>
              <PointPlacement>
                <AnchorPoint><AnchorPointX>0.5</AnchorPointX><AnchorPointY>0.0</AnchorPointY></AnchorPoint>
                <Displacement><DisplacementX>0</DisplacementX><DisplacementY>12</DisplacementY></Displacement>
              </PointPlacement>
            </LabelPlacement>
            <Halo>
              <Radius>2</Radius>
              <Fill><CssParameter name="fill">#ffffff</CssParameter></Fill>
            </Halo>
            <Fill><CssParameter name="fill">#334155</CssParameter></Fill>
            <VendorOption name="conflictResolution">true</VendorOption>
          </TextSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
