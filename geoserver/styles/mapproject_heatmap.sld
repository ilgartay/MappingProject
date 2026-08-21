<?xml version="1.0" encoding="UTF-8"?>
<!--
  Isi haritasi stili.

  Isin ozu <Transformation> blogu: GeoServer, noktalari cizmeden once
  Heatmap donusumunden geciriyor. Donusum noktalardan bir yogunluk
  rasteri uretiyor, RasterSymbolizer da o rasteri renklendiriyor.
  Yani yogunluk hesabi istemcide degil sunucuda yapiliyor.

  Fonksiyonun adi "vec:Heatmap", belgelerde sik gecen "gs:Heatmap" degil.
  gs: on eki WPS eklentisiyle geliyor; biz WPS kurmadik. HeatmapProcess
  org.geotools.process.vector paketinde oldugu icin vec: altinda kayitli.
  Yanlis on ek "Unable to find function" hatasi veriyor.

  wms_bbox / wms_width / wms_height, GeoServer'in her WMS isteginde
  doldurdugu ortam degiskenleri. Bunlar olmadan donusum hangi alani
  hangi cozunurlukte hesaplayacagini bilemez.
-->
<StyledLayerDescriptor version="1.0.0"
    xmlns="http://www.opengis.net/sld"
    xmlns:ogc="http://www.opengis.net/ogc"
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:schemaLocation="http://www.opengis.net/sld http://schemas.opengis.net/sld/1.0.0/StyledLayerDescriptor.xsd">
  <NamedLayer>
    <Name>mapproject_heatmap</Name>
    <UserStyle>
      <Title>MapProject isi haritasi</Title>
      <FeatureTypeStyle>
        <Transformation>
          <ogc:Function name="vec:Heatmap">
            <ogc:Function name="parameter">
              <ogc:Literal>data</ogc:Literal>
            </ogc:Function>
            <!--
              Etki yaricapi (piksel). Buyuk deger daha yumusak, genis lekeler
              uretir; kucuk deger noktalari birbirinden ayirir. env ile
              disaridan gecilebiliyor: WMS istegine env=radius:40 eklemek yeter.
            -->
            <ogc:Function name="parameter">
              <ogc:Literal>radiusPixels</ogc:Literal>
              <ogc:Function name="env">
                <ogc:Literal>radius</ogc:Literal>
                <ogc:Literal>35</ogc:Literal>
              </ogc:Function>
            </ogc:Function>
            <!-- Hesap cozunurlugu: 10 piksellik hucrelerde hesaplanip buyutuluyor. -->
            <ogc:Function name="parameter">
              <ogc:Literal>pixelsPerCell</ogc:Literal>
              <ogc:Literal>10</ogc:Literal>
            </ogc:Function>
            <ogc:Function name="parameter">
              <ogc:Literal>outputBBOX</ogc:Literal>
              <ogc:Function name="env"><ogc:Literal>wms_bbox</ogc:Literal></ogc:Function>
            </ogc:Function>
            <ogc:Function name="parameter">
              <ogc:Literal>outputWidth</ogc:Literal>
              <ogc:Function name="env"><ogc:Literal>wms_width</ogc:Literal></ogc:Function>
            </ogc:Function>
            <ogc:Function name="parameter">
              <ogc:Literal>outputHeight</ogc:Literal>
              <ogc:Function name="env"><ogc:Literal>wms_height</ogc:Literal></ogc:Function>
            </ogc:Function>
          </ogc:Function>
        </Transformation>
        <Rule>
          <RasterSymbolizer>
            <!--
              Yogunluk her zaman 0-1 arasina olceklenir; renk basamaklari da
              bu araliga gore. Ekrandaki lejant birebir bu degerleri gosteriyor.
              0 tamamen saydam: veri olmayan yer haritayi kapatmasin.
            -->
            <Opacity>0.75</Opacity>
            <ColorMap type="ramp">
              <ColorMapEntry color="#2c7bb6" quantity="0.0"  label="0.0" opacity="0"/>
              <ColorMapEntry color="#2c7bb6" quantity="0.2"  label="0.2" opacity="0.7"/>
              <ColorMapEntry color="#abd9e9" quantity="0.4"  label="0.4" opacity="0.8"/>
              <ColorMapEntry color="#ffffbf" quantity="0.6"  label="0.6" opacity="0.85"/>
              <ColorMapEntry color="#fdae61" quantity="0.8"  label="0.8" opacity="0.9"/>
              <ColorMapEntry color="#d7191c" quantity="1.0"  label="1.0" opacity="1"/>
            </ColorMap>
          </RasterSymbolizer>
        </Rule>
      </FeatureTypeStyle>
    </UserStyle>
  </NamedLayer>
</StyledLayerDescriptor>
