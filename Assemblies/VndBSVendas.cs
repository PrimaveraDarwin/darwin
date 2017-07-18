using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data.Common;
using UpgradeHelpers.DB.ADO;
using UpgradeHelpers.Helpers;
using VndBE100;
using static BasBE100.BasBETiposGcp;
using StdBE100;
using BasBE100;
using IVndBS100;

namespace VndBS100
{
	//UPGRADE_NOTE: (1043) Class instancing was changed to public. More Information: http://www.vbtonet.com/ewis/ewi1043.aspx 
	public class VndBSVendas
	: IVndBS100.IVndBSVendas
	{

		//---------------------------------------------------------------------------------------
		//Module: VndBSVendas
		//Purpose:
		//---------------------------------------------------------------------------------------


		private const int C_DIMARRAY_PRECOS = 35; //CR.687

		private struct InfoDocTrans
		{
			public string DocTrans;
			public string TipoDocTrans;
			public double TotalPendente;
			public string PagarReceber;
			public bool LimiteCredito;

			public bool LigaCC;
			public string FilialTrans;
			public string DocumentoTrans;
			public string SerieTrans;
			public int NumDocTrans;

			public string CondPagTrans;
			public static InfoDocTrans CreateInstance()
			{
				InfoDocTrans result = new InfoDocTrans();
				result.DocTrans = String.Empty;
				result.TipoDocTrans = String.Empty;
				result.PagarReceber = String.Empty;
				result.FilialTrans = String.Empty;
				result.DocumentoTrans = String.Empty;
				result.SerieTrans = String.Empty;
				result.CondPagTrans = String.Empty;
				return result;
			}
		}

		private ErpBS100.ErpBS m_objErpBSO = null;

		private StdBE100.StdBEDefCamposUtil m_objDefCamposUtil = null;
		private StdBE100.StdBEDefCamposUtil m_objDefCamposUtilLinha = null;
		private StdBE100.StdBEDefCamposUtil m_objDefCamposUtilHistorico = null;

		//CS. 2970 - Certificação nas Compras
		private clsCertificacaoSoftware m_objCertificacaoSoftware = null;

		//'Epic 406
		private FuncoesComuns100.clsBSContratos m_objContratos = null;
		private OrderedDictionary m_ColLigaCamposUtilTranf = null;

		//US 27510 - Consumo automatico de lotes na conversão de documentos
		clsLotesAuto m_clsLotesAuto = null;

		//---------------------------------------------------------------------------------------
		// Procedure   : Class_Terminate
		// Description :
		// Returns     : None
		//---------------------------------------------------------------------------------------
		~VndBSVendas()
		{

			m_objErpBSO = null;

			m_objDefCamposUtil = null;
			m_objDefCamposUtilLinha = null;
			m_objDefCamposUtilHistorico = null;
			//CS. 2970 - Certificação nas Compras
			m_objCertificacaoSoftware = null;
			m_ColLigaCamposUtilTranf = null;

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : ErpBSO
		// Description :
		// Arguments   : objValue -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		internal ErpBS100.ErpBS ErpBSO
		{
			set
			{

				m_objErpBSO = value;
				m_ColLigaCamposUtilTranf = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

			}
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : CertificacaoSoftware
		// Description : CS. 2970 - Certificação nas Compras
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private clsCertificacaoSoftware CertificacaoSoftware
		{
			get
			{

				if (m_objCertificacaoSoftware == null)
				{

					m_objCertificacaoSoftware = new clsCertificacaoSoftware();
					m_objCertificacaoSoftware.Motor = m_objErpBSO;
					m_objCertificacaoSoftware.Modulo = ConstantesPrimavera100.Modulos.Vendas;

				}

				return m_objCertificacaoSoftware;

			}
		}


		//Epic 406
		//---------------------------------------------------------------------------------------
		// Procedure     : Contratos
		// Description   : Classe que implementa as regras de negócio para a utilização dos contratos
		//---------------------------------------------------------------------------------------
		private FuncoesComuns100.clsBSContratos Contratos
		{
			get
			{

				if (m_objContratos == null)
				{

					m_objContratos = FuncoesComuns100.FuncoesBS.Instancia_Contratos;


					m_objContratos.Modulo = ConstantesPrimavera100.Modulos.Vendas;


				}

				return m_objContratos;

			}
		}


		//---------------------------------------------------------------------------------------
		// Procedure     : DefCamposUtilHistorico
		// Description   :
		//---------------------------------------------------------------------------------------
		internal StdBE100.StdBEDefCamposUtil DefCamposUtilHistorico
		{
			get
			{

				if (m_objDefCamposUtilHistorico == null)
				{

					if (FuncoesComuns100.FuncoesBS.Utils.PossuiExtensibilidadeBD())
					{
						m_objDefCamposUtilHistorico = (StdBE100.StdBEDefCamposUtil) m_objErpBSO.PagamentosRecebimentos.Historico.DaDefCamposUtil();
					}
					else
					{
						m_objDefCamposUtilHistorico = new StdBE100.StdBEDefCamposUtil();
					}
				}

				return m_objDefCamposUtilHistorico;

			}
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : ValidaActualizacaoRascunho
		// Description : Hades CS.1577 - Faz as validações básicas para a gravação de um documento em modo rascunho
		// Arguments   : clsDocumentoVenda -->
		// Arguments   : strErros          -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private bool ValidaActualizacaoRascunho(VndBE100.VndBEDocumentoVenda clsDocumentoVenda, ref string strErros)
		{

			bool result = false;
			result = true;


			//Já tem assinatura?
			if (Strings.Len(clsDocumentoVenda.Assinatura) > 0)
			{

				result = false;
				strErros = strErros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16608, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

			}

			if (clsDocumentoVenda.EmModoEdicao)
			{

				//Não era rascunho e está em modo de edição??
				if (!clsDocumentoVenda.AntRascunho)
				{

					result = false;
					strErros = strErros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15785, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}

				//Está em modo de Edição, mas não existe 1 rascunho...
				string tempRefParam = clsDocumentoVenda.ID;
				if (!m_objErpBSO.Vendas.Documentos.ExisteRascunhoID(tempRefParam))
				{

					result = false;
					strErros = strErros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15786, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}

			}

			//Não era rascunho e está em modo de edição??
			string tempRefParam2 = clsDocumentoVenda.ID;
			if (m_objErpBSO.Vendas.Documentos.ExisteID(tempRefParam2))
			{

				result = false;
				strErros = strErros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15787, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

			}

			//BID 593787 : Impedir a gravação em séries inactivas
			if (ReflectionHelper.GetPrimitiveValue<bool>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, "SerieInactiva")))
			{

				result = false;
				string tempRefParam3 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(11427, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
				dynamic[] tempRefParam4 = new dynamic[]{clsDocumentoVenda.Serie, clsDocumentoVenda.Tipodoc};
				strErros = strErros + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam3, tempRefParam4) + Environment.NewLine;

			}


			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ActualizaRascunho
		// Description : Hades CS.1577 - Actualiza um documento de Rascunho
		// Arguments   : clsDocumentoVenda -->
		// Arguments   : strAvisos         -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void ActualizaRascunho(VndBEDocumentoVenda clsDocumentoVenda, string strAvisos)
		{
			VndBE100.VndBETabVenda clsTabVenda = null;
			int intMult = 0;
			string StrErro = "";
			bool blnIniciouTrans = false;
			//BID 599506

			try
			{


				if (ValidaActualizacaoRascunho(clsDocumentoVenda, ref StrErro))
				{

					//Valores por omissão do documento
					clsDocumentoVenda.Rascunho = true;
					clsDocumentoVenda.DataGravacao = DateTime.Now;
					clsDocumentoVenda.Utilizador = m_objErpBSO.Contexto.UtilizadorActual;

					//Retira o próximo numerador disponivel para o rascunho
					if (!clsDocumentoVenda.EmModoEdicao)
					{ //BID 592410 : permite garantir que o documento de venda já gravado em rascunho vai manter o mesmo NumDoc se for novamente gravado em rascunho
						string tempRefParam = clsDocumentoVenda.Tipodoc;
						string tempRefParam2 = clsDocumentoVenda.Serie;
						string tempRefParam3 = clsDocumentoVenda.Filial;
						clsDocumentoVenda.NumDoc = m_objErpBSO.DSO.Vendas.Documentos.DevolveProximoNumDocRascunho(tempRefParam, ref tempRefParam2, ref tempRefParam3);
					}

					//Edita a configuração do documento de venda
					string tempRefParam4 = clsDocumentoVenda.Tipodoc;
					clsTabVenda = m_objErpBSO.Vendas.TabVendas.Edita(tempRefParam4);

					//** Preenche o objecto documento de venda com os dados por defeito
					PreencheDocVenda(ref clsDocumentoVenda, clsTabVenda);

					//Preenche o lancamento
					if (Strings.Len(clsDocumentoVenda.TipoLancamento) == 0)
					{

						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Series.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						clsDocumentoVenda.TipoLancamento = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, "TipoLancamento"));

					}

					//Calcular os totais --> Validar se é necessário
					CalculaValoresTotais(ref clsDocumentoVenda);

					//CR.141 - Dá o factor de multiplicação do documento
					intMult = FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc);

					//Inicia a transacção
					IniciaTransaccao(ref blnIniciouTrans);

					//Aplica o Multiplicador de sinal nas linhas
					AplicaMultiplicador(intMult, clsDocumentoVenda);

					//*********************************************************************
					//Grava na Base de Dados
					m_objErpBSO.DSO.Vendas.Documentos.ActualizaRascunho(ref clsDocumentoVenda);
					//*********************************************************************

					//BID 599506
					if (clsDocumentoVenda.Callbacks != null)
					{
						foreach (StdBE100.IStdBECallback objCallback in clsDocumentoVenda.Callbacks)
						{
							objCallback.Callback(StdBE100.StdBETipos.EnumTipoEventoCallback.ecAntesDeTerminarTransacao, clsDocumentoVenda);
						}
					}
					//^BID 599506

					//Termina a transacção
					m_objErpBSO.TerminaTransaccao();

					blnIniciouTrans = false;

				}
				else
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.ActualizaRascunho", StrErro);

				}


				clsTabVenda = null;
			}
			catch (Exception e)
			{


				//Desfaz a transacção
				if (blnIniciouTrans)
				{

					m_objErpBSO.DesfazTransaccao();
					blnIniciouTrans = false;

				}

				//Tratamento de Erros
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				if (Information.Err().Number == StdErros.StdErroPrevisto)
				{

					if (StrErro == e.Message)
					{

						//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
						StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaRascunho", e.Message);

					}
					else
					{

						//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
						StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaRascunho", StrErro + Environment.NewLine + e.Message);

					}

				}
				else
				{

					//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
					StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaRascunho", e.Message);

				}
			}

		}

		public void ActualizaRascunho(VndBEDocumentoVenda clsDocumentoVenda)
		{
			string tempRefParam4 = "";
			ActualizaRascunho(clsDocumentoVenda, tempRefParam4);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : AplicaMultiplicador
		// Description : Hades CS.1577 - Aplica o Multiplicador no documento
		// Arguments   : intMultiplicador  -->
		// Arguments   : clsDocumentoVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void AplicaMultiplicador(int intMultiplicador, VndBE100.VndBEDocumentoVenda clsDocumentoVenda, bool AplicaCabecalho = true, bool AplicaLinhas = true)
		{
			dynamic objResumoRetencao = null;
			StdBE100.StdBECampos objBECampos = null;

			try
			{


				if (AplicaCabecalho)
				{

					//** Actualiza o cabeçalho do documento de venda
					clsDocumentoVenda.TotalDesc *= intMultiplicador;
					clsDocumentoVenda.TotalIva *= intMultiplicador;
					clsDocumentoVenda.TotalRecargo *= intMultiplicador;
					clsDocumentoVenda.TotalMerc *= intMultiplicador;
					clsDocumentoVenda.TotalOutros *= intMultiplicador; // retirado o abs
					clsDocumentoVenda.TotalEcotaxa *= intMultiplicador;
					//BID 594827
					foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda in clsDocumentoVenda.Linhas)
					{

						//Controlar a existência de adiantamentos no documento:
						//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
						if (ClsLinhaVenda.TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAdiantamentos && !Convert.IsDBNull(ClsLinhaVenda.IdLinhaEstorno))
						{

							//Ver se é um adiantamento de CC
							string tempRefParam = ClsLinhaVenda.IdLinhaEstorno;
							dynamic[] tempRefParam2 = new dynamic[]{"IDHistorico"};
							objBECampos = m_objErpBSO.Vendas.Documentos.DaValorAtributosIDLinha(tempRefParam, tempRefParam2);

							if (objBECampos != null)
							{

								//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
								string tempRefParam3 = "IdHistorico";
								string tempRefParam4 = "IdHistorico";
								if (!Convert.IsDBNull(objBECampos.GetItem(ref tempRefParam3).Valor) && Strings.Len(ReflectionHelper.GetPrimitiveValue<string>(objBECampos.GetItem(ref tempRefParam4).Valor)) > 0)
								{

									clsDocumentoVenda.TotalDocumento = clsDocumentoVenda.TotalDocumento + ClsLinhaVenda.TotalIliquido + ClsLinhaVenda.TotalIva;

								}

							}

							objBECampos = null;

						}

					}
					clsDocumentoVenda.TotalDocumento *= intMultiplicador;
					clsDocumentoVenda.TotalIEC *= intMultiplicador;
					clsDocumentoVenda.TotalIS *= intMultiplicador;

					if (clsDocumentoVenda.Retencoes != null)
					{

						clsDocumentoVenda.TotalRetencao = 0;
						clsDocumentoVenda.TotalRetencaoGarantia = 0;

						foreach (dynamic objResumoRetencao2 in clsDocumentoVenda.Retencoes)
						{
							objResumoRetencao = objResumoRetencao2;

							if ((~Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Licenca.ContasCorrentes.CCorrenteEstendida) & ((!m_objErpBSO.Base.Licenca.ConfiguracaoStarterEasy) ? -1 : 0)) != 0)
							{

								objResumoRetencao.TipoEntidadeRetencao = objResumoRetencao.TipoEntidade;
								objResumoRetencao.EntidadeRetencao = objResumoRetencao.Entidade;

							}

							if (objResumoRetencao.TipoRetencao == ((int) CctBE100.CctBETipos.TipoRetencao.Garantia))
							{

								objResumoRetencao.Valor *= intMultiplicador;
								objResumoRetencao.Incidencia *= intMultiplicador;
								clsDocumentoVenda.TotalRetencaoGarantia += objResumoRetencao.Valor;
							}
							else
							{

								objResumoRetencao.Valor *= intMultiplicador;
								objResumoRetencao.Incidencia *= intMultiplicador;
								clsDocumentoVenda.TotalRetencao += objResumoRetencao.Valor;

							}

							objResumoRetencao = null;
						}


					}

				}

				if (AplicaLinhas)
				{

					//**** Linhas ****
					foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda in clsDocumentoVenda.Linhas)
					{

						AplicaMultiplicadorLinha(ClsLinhaVenda, intMultiplicador);

					}

				}


				objResumoRetencao = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_AplicaMultiplicador", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ActualizaTabelasRascunhos
		// Description :
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void ActualizaTabelasRascunhos()
		{

			try
			{

				m_objErpBSO.IniciaTransaccao();

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaTabelasRascunhos();

				m_objErpBSO.TerminaTransaccao();
			}
			catch (System.Exception excep)
			{

				m_objErpBSO.DesfazTransaccao();
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_ActualizaTabelasRascunhos", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_AnulaDocumento
		// Description :
		// Arguments   : Filial   -->
		// Arguments   : TipoDoc  -->
		// Arguments   : strSerie -->
		// Arguments   : NumDoc   -->
		// Arguments   : Avisos   -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void AnulaDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc, string Motivo, string Observacoes, string Avisos)
		{

			string tempRefParam = "Id";
			string strIdDoc = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.Vendas.Documentos.DaValorAtributo(Filial, TipoDoc, strSerie, Numdoc, tempRefParam));
			AnulaDocumentoID(strIdDoc, Motivo, Observacoes, Avisos);

		}

		public void AnulaDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc, string Motivo, string Observacoes)
		{
			string tempRefParam5 = "";
			AnulaDocumento(Filial, TipoDoc, strSerie, Numdoc, Motivo, Observacoes, tempRefParam5);
		}

		public void AnulaDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc, string Motivo)
		{
			string tempRefParam6 = "";
			AnulaDocumento(Filial, TipoDoc, strSerie, Numdoc, Motivo, "", tempRefParam6);
		}

		public void AnulaDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{
			string tempRefParam7 = "";
			AnulaDocumento(Filial, TipoDoc, strSerie, Numdoc, "", "", tempRefParam7);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_AnulaDocumentoID
		// Description :
		// Arguments   : Id     -->
		// Arguments   : Avisos -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void AnulaDocumentoID(string Id, string Motivo, string Observacoes, string Avisos)
		{
			string strErrosValidacao = "";
			bool blnInTran = false;

			//Valida a remoção dos documentos...
			try
			{

				if (!ValidaAnulacaoDocumentoID(Id, Motivo, strErrosValidacao))
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_IVndBSVendas_AnulaDocumentoID", strErrosValidacao);

				}
				else
				{

					m_objErpBSO.IniciaTransaccao();
					blnInTran = true;

					string tempRefParam = "";
					AbreLinhasECLFechadasAnulacao(ref Id, ref tempRefParam);
					//É valido? então segue...
					FuncoesComuns100.FuncoesBS.Documentos.AnulaDocumentoLogistica(Id, ConstantesPrimavera100.Modulos.Vendas, Motivo, Observacoes, ref Avisos);

					m_objErpBSO.TerminaTransaccao();
					blnInTran = false;

				}
			}
			catch (System.Exception excep)
			{

				if (blnInTran)
				{

					m_objErpBSO.DesfazTransaccao();

				}
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_AnulaDocumentoID", excep.Message);
			}

		}

		public void AnulaDocumentoID(string Id, string Motivo, string Observacoes)
		{
			string tempRefParam8 = "";
			AnulaDocumentoID(Id, Motivo, Observacoes, tempRefParam8);
		}

		public void AnulaDocumentoID(string Id, string Motivo)
		{
			string tempRefParam9 = "";
			AnulaDocumentoID(Id, Motivo, "", tempRefParam9);
		}

		public void AnulaDocumentoID(string Id)
		{
			string tempRefParam10 = "";
			AnulaDocumentoID(Id, "", "", tempRefParam10);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_CopiaLinhas
		// Description :
		// Arguments   : objOrigem              -->
		// Arguments   : objDestino             -->
		// Arguments   : blnSugereDadosEntidade -->
		// Arguments   : blnCopiaPrecoUnitario  -->
		// Arguments   : strTipoEntidade        -->
		// Arguments   : strEntidade            -->
		// Arguments   : strTipoDoc             -->
		// Arguments   : strSerie               -->
		// Arguments   : Estorno                -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc, string strSerie, bool Estorno, bool GravaDocumento, string AvisosGravacao)
		{
			int[] arrNumLinhaCopiar = null;
			double[] arrNumQuantCopiar = null;
			int lngPosArray = 0;

			try
			{

				if (objLinhasCopiar.NumItens > 0)
				{

					arrNumLinhaCopiar = new int[objLinhasCopiar.NumItens];
					arrNumQuantCopiar = new double[objLinhasCopiar.NumItens];
					lngPosArray = 0;

					foreach (StdBE100.StdBEValorVar ObjLinha in objLinhasCopiar)
					{

						arrNumLinhaCopiar[lngPosArray] = m_objErpBSO.DSO.Plat.Utils.FLng(ObjLinha.Chave);
						arrNumQuantCopiar[lngPosArray] = m_objErpBSO.DSO.Plat.Utils.FDbl(ObjLinha.Valor);

						lngPosArray++;

					}

					m_objErpBSO.Internos.Documentos.CopiaLinhas(ConstantesPrimavera100.Modulos.Vendas, objOrigem, ConstantesPrimavera100.Modulos.Vendas, objDestino, arrNumLinhaCopiar, arrNumQuantCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, strSerie, Estorno);

					if (GravaDocumento)
					{

						Actualiza(objDestino, AvisosGravacao);

					}

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_CopiaLinhas", excep.Message);
			}

		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc, string strSerie, bool Estorno, bool GravaDocumento)
		{
			string tempRefParam11 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, strSerie, Estorno, GravaDocumento, tempRefParam11);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc, string strSerie, bool Estorno)
		{
			bool tempRefParam12 = false;
			string tempRefParam13 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, strSerie, Estorno, tempRefParam12, tempRefParam13);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc, string strSerie)
		{
			bool tempRefParam14 = false;
			bool tempRefParam15 = false;
			string tempRefParam16 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, strSerie, tempRefParam14, tempRefParam15, tempRefParam16);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc)
		{
			string tempRefParam17 = "";
			bool tempRefParam18 = false;
			bool tempRefParam19 = false;
			string tempRefParam20 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, tempRefParam17, tempRefParam18, tempRefParam19, tempRefParam20);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade)
		{
			string tempRefParam21 = "";
			string tempRefParam22 = "";
			bool tempRefParam23 = false;
			bool tempRefParam24 = false;
			string tempRefParam25 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, tempRefParam21, tempRefParam22, tempRefParam23, tempRefParam24, tempRefParam25);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade)
		{
			string tempRefParam26 = "";
			string tempRefParam27 = "";
			string tempRefParam28 = "";
			bool tempRefParam29 = false;
			bool tempRefParam30 = false;
			string tempRefParam31 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, tempRefParam26, tempRefParam27, tempRefParam28, tempRefParam29, tempRefParam30, tempRefParam31);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario)
		{
			string tempRefParam32 = "";
			string tempRefParam33 = "";
			string tempRefParam34 = "";
			string tempRefParam35 = "";
			bool tempRefParam36 = false;
			bool tempRefParam37 = false;
			string tempRefParam38 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, tempRefParam32, tempRefParam33, tempRefParam34, tempRefParam35, tempRefParam36, tempRefParam37, tempRefParam38);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem, VndBEDocumentoVenda objDestino, StdBEValoresVar objLinhasCopiar, bool blnSugereDadosEntidade)
		{
			bool tempRefParam39 = true;
			string tempRefParam40 = "";
			string tempRefParam41 = "";
			string tempRefParam42 = "";
			string tempRefParam43 = "";
			bool tempRefParam44 = false;
			bool tempRefParam45 = false;
			string tempRefParam46 = "";
			CopiaLinhas(objOrigem, objDestino, objLinhasCopiar, blnSugereDadosEntidade, tempRefParam39, tempRefParam40, tempRefParam41, tempRefParam42, tempRefParam43, tempRefParam44, tempRefParam45, tempRefParam46);
		}

		public void CopiaLinhas(VndBEDocumentoVenda objOrigem,  VndBEDocumentoVenda objDestino,  StdBEValoresVar objLinhasCopiar)
		{
			bool tempRefParam47 = false;
			bool tempRefParam48 = true;
			string tempRefParam49 = "";
			string tempRefParam50 = "";
			string tempRefParam51 = "";
			string tempRefParam52 = "";
			bool tempRefParam53 = false;
			bool tempRefParam54 = false;
			string tempRefParam55 = "";
			CopiaLinhas( objOrigem,  objDestino,  objLinhasCopiar,  tempRefParam47,  tempRefParam48,  tempRefParam49,  tempRefParam50,  tempRefParam51,  tempRefParam52,  tempRefParam53,  tempRefParam54,  tempRefParam55);
		}

		public void CopiaLinhasEX2( VndBE100.VndBEDocumentoVenda objOrigem,  VndBE100.VndBEDocumentoVenda objDestino,  string LinhasACopiar,  string QuantidadesACopiar,  bool blnSugereDadosEntidade,  bool blnCopiaPrecoUnitario,  string strTipoEntidade,  string strEntidade,  string strTipoDoc,  string strSerie,  bool Estorno,  bool GravaDocumento,  string AvisosGravacao)
		{
			StdBE100.StdBEValoresVar objLinhas = null;
			int lngLinha = 0;
			double dblQuantidade = 0;

			string[] strArrLinhas = (string[]) LinhasACopiar.Split('@');
			string[] strArrQuant = (string[]) QuantidadesACopiar.Split('@');

			if (strArrLinhas.GetUpperBound(0) > 0)
			{

				if (strArrLinhas.GetUpperBound(0) != strArrQuant.GetUpperBound(0))
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IVndBSVendas_CopiaLinhasEX", "As quantidades e as linhas definidas não estão coerentes.");

				}

				objLinhas = new StdBE100.StdBEValoresVar();

				for (int intIndex = 0; intIndex <= strArrLinhas.GetUpperBound(0); intIndex++)
				{

					lngLinha = m_objErpBSO.DSO.Plat.Utils.FLng(strArrLinhas[intIndex]);
					dblQuantidade = m_objErpBSO.DSO.Plat.Utils.FDbl(strArrQuant[intIndex]);

					if (dblQuantidade != 0 && lngLinha > 0)
					{

						objLinhas.InsereNovo(m_objErpBSO.DSO.Plat.Utils.FStr(lngLinha), dblQuantidade);

					}

				}

				if (objLinhas.NumItens > 0)
				{

					CopiaLinhas( objOrigem,  objDestino,  objLinhas,  blnSugereDadosEntidade,  blnCopiaPrecoUnitario,  strTipoEntidade,  strEntidade,  strTipoDoc,  strSerie,  Estorno,  GravaDocumento,  AvisosGravacao);

				}

			}


		}

		public void CopiaLinhasEX2( VndBE100.VndBEDocumentoVenda objOrigem,  VndBE100.VndBEDocumentoVenda objDestino,  string LinhasACopiar,  string QuantidadesACopiar,  bool blnSugereDadosEntidade,  bool blnCopiaPrecoUnitario,  string strTipoEntidade,  string strEntidade,  string strTipoDoc,  string strSerie,  bool Estorno,  bool GravaDocumento)
		{
			string tempRefParam56 = "";
			CopiaLinhasEX2( objOrigem,  objDestino,  LinhasACopiar,  QuantidadesACopiar,  blnSugereDadosEntidade,  blnCopiaPrecoUnitario,  strTipoEntidade,  strEntidade,  strTipoDoc,  strSerie,  Estorno,  GravaDocumento,  tempRefParam56);
		}

		public void CopiaLinhasEX2( VndBE100.VndBEDocumentoVenda objOrigem,  VndBE100.VndBEDocumentoVenda objDestino,  string LinhasACopiar,  string QuantidadesACopiar,  bool blnSugereDadosEntidade,  bool blnCopiaPrecoUnitario,  string strTipoEntidade,  string strEntidade,  string strTipoDoc,  string strSerie,  bool Estorno)
		{
			bool tempRefParam57 = false;
			string tempRefParam58 = "";
			CopiaLinhasEX2( objOrigem,  objDestino,  LinhasACopiar,  QuantidadesACopiar,  blnSugereDadosEntidade,  blnCopiaPrecoUnitario,  strTipoEntidade,  strEntidade,  strTipoDoc,  strSerie,  Estorno,  tempRefParam57,  tempRefParam58);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc, string strSerie)
		{
			bool tempRefParam59 = false;
			bool tempRefParam60 = false;
			string tempRefParam61 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, strSerie, tempRefParam59, tempRefParam60, tempRefParam61);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade, string strTipoDoc)
		{
			string tempRefParam62 = "";
			bool tempRefParam63 = false;
			bool tempRefParam64 = false;
			string tempRefParam65 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, strTipoDoc, tempRefParam62, tempRefParam63, tempRefParam64, tempRefParam65);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade, string strEntidade)
		{
			string tempRefParam66 = "";
			string tempRefParam67 = "";
			bool tempRefParam68 = false;
			bool tempRefParam69 = false;
			string tempRefParam70 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, strEntidade, tempRefParam66, tempRefParam67, tempRefParam68, tempRefParam69, tempRefParam70);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario, string strTipoEntidade)
		{
			string tempRefParam71 = "";
			string tempRefParam72 = "";
			string tempRefParam73 = "";
			bool tempRefParam74 = false;
			bool tempRefParam75 = false;
			string tempRefParam76 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, strTipoEntidade, tempRefParam71, tempRefParam72, tempRefParam73, tempRefParam74, tempRefParam75, tempRefParam76);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade, bool blnCopiaPrecoUnitario)
		{
			string tempRefParam77 = "";
			string tempRefParam78 = "";
			string tempRefParam79 = "";
			string tempRefParam80 = "";
			bool tempRefParam81 = false;
			bool tempRefParam82 = false;
			string tempRefParam83 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, blnCopiaPrecoUnitario, tempRefParam77, tempRefParam78, tempRefParam79, tempRefParam80, tempRefParam81, tempRefParam82, tempRefParam83);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar, bool blnSugereDadosEntidade)
		{
			bool tempRefParam84 = true;
			string tempRefParam85 = "";
			string tempRefParam86 = "";
			string tempRefParam87 = "";
			string tempRefParam88 = "";
			bool tempRefParam89 = false;
			bool tempRefParam90 = false;
			string tempRefParam91 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, blnSugereDadosEntidade, tempRefParam84, tempRefParam85, tempRefParam86, tempRefParam87, tempRefParam88, tempRefParam89, tempRefParam90, tempRefParam91);
		}

		public void CopiaLinhasEX2(VndBE100.VndBEDocumentoVenda objOrigem, VndBE100.VndBEDocumentoVenda objDestino, string LinhasACopiar, string QuantidadesACopiar)
		{
			bool tempRefParam92 = false;
			bool tempRefParam93 = true;
			string tempRefParam94 = "";
			string tempRefParam95 = "";
			string tempRefParam96 = "";
			string tempRefParam97 = "";
			bool tempRefParam98 = false;
			bool tempRefParam99 = false;
			string tempRefParam100 = "";
			CopiaLinhasEX2(objOrigem, objDestino, LinhasACopiar, QuantidadesACopiar, tempRefParam92, tempRefParam93, tempRefParam94, tempRefParam95, tempRefParam96, tempRefParam97, tempRefParam98, tempRefParam99, tempRefParam100);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ImprimeDocumento
		// Description :
		// Arguments   : TipoDoc            -->
		// Arguments   : Serie              -->
		// Arguments   : NumDoc             -->
		// Arguments   : Filial             -->
		// Arguments   : Numvias            -->
		// Arguments   : NomeReport         -->
		// Arguments   : SegundaVia         -->
		// Arguments   : DestinoPDF         -->
		// Arguments   : EntidadeFacturacao -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool ImprimeDocumento(string TipoDoc, string Serie, int Numdoc, string Filial, int Numvias, string NomeReport, bool SegundaVia, string DestinoPDF, int EntidadeFacturacao)
		{

			string tempRefParam = ConstantesPrimavera100.Modulos.Vendas;
			return m_objErpBSO.Base.FuncoesGlobais.ImprimeDocumento(ref tempRefParam, ref TipoDoc, ref Serie, ref Numdoc, ref Filial, Numvias, NomeReport, SegundaVia, DestinoPDF, EntidadeFacturacao);

		}

		public bool ImprimeDocumento( string TipoDoc,  string Serie,  int Numdoc,  string Filial,  short Numvias,  string NomeReport,  bool SegundaVia,  string DestinoPDF)
		{
			short tempRefParam101 = 1;
			return ImprimeDocumento( TipoDoc,  Serie,  Numdoc,  Filial,  Numvias,  NomeReport,  SegundaVia,  DestinoPDF,  tempRefParam101);
		}

		public bool ImprimeDocumento( string TipoDoc,  string Serie,  int Numdoc,  string Filial,  short Numvias,  string NomeReport,  bool SegundaVia)
		{
			string tempRefParam102 = "";
			short tempRefParam103 = 1;
			return ImprimeDocumento( TipoDoc,  Serie,  Numdoc,  Filial,  Numvias,  NomeReport,  SegundaVia,  tempRefParam102,  tempRefParam103);
		}

		public bool ImprimeDocumento( string TipoDoc,  string Serie,  int Numdoc,  string Filial,  short Numvias,  string NomeReport)
		{
			bool tempRefParam104 = false;
			string tempRefParam105 = "";
			short tempRefParam106 = 1;
			return ImprimeDocumento( TipoDoc,  Serie,  Numdoc,  Filial,  Numvias,  NomeReport,  tempRefParam104,  tempRefParam105,  tempRefParam106);
		}

		public bool ImprimeDocumento( string TipoDoc,  string Serie,  int Numdoc,  string Filial,  short Numvias)
		{
			string tempRefParam107 = "";
			bool tempRefParam108 = false;
			string tempRefParam109 = "";
			short tempRefParam110 = 1;
			return ImprimeDocumento( TipoDoc,  Serie,  Numdoc,  Filial,  Numvias,  tempRefParam107,  tempRefParam108,  tempRefParam109,  tempRefParam110);
		}

		public bool ImprimeDocumento( string TipoDoc,  string Serie,  int Numdoc,  string Filial)
		{
			short tempRefParam111 = 0;
			string tempRefParam112 = "";
			bool tempRefParam113 = false;
			string tempRefParam114 = "";
			short tempRefParam115 = 1;
			return ImprimeDocumento( TipoDoc,  Serie,  Numdoc,  Filial,  tempRefParam111,  tempRefParam112,  tempRefParam113,  tempRefParam114,  tempRefParam115);
		}

		public VndBEDocumentoVenda PreencheImpostoSelo(VndBEDocumentoVenda BEDocumentoVenda, int NumLinha, BasBESelo BESelo)
		{

			return (VndBEDocumentoVenda) m_objErpBSO.Base.FuncoesGlobais.PreencheImpostoSelo(BEDocumentoVenda, NumLinha, ref BESelo);

		}

		public VndBEDocumentoVenda PreencheImpostoSelo(VndBEDocumentoVenda BEDocumentoVenda, int NumLinha)
		{
			BasBESelo tempRefParam116 = null;
			return PreencheImpostoSelo(BEDocumentoVenda, NumLinha, tempRefParam116);
		}

		public VndBEDocumentoVenda PreencheImpostoSelo(VndBEDocumentoVenda BEDocumentoVenda)
		{
			BasBESelo tempRefParam117 = null;
			return PreencheImpostoSelo(BEDocumentoVenda, 0, tempRefParam117);
		}

		public BasBEResumosIS PreencheResumoIS(VndBEDocumentoVenda BEDocumentoVenda, double TotalIS)
		{

			return m_objErpBSO.Base.FuncoesGlobais.PreencheResumoIS(BEDocumentoVenda, ref TotalIS);

		}

		public BasBEResumosIS PreencheResumoIS(VndBEDocumentoVenda BEDocumentoVenda)
		{
			double tempRefParam118 = 0;
			return PreencheResumoIS(BEDocumentoVenda, tempRefParam118);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributoLock
		// Description :
		// Arguments   : Filial   -->
		// Arguments   : TipoDoc  -->
		// Arguments   : Serie    -->
		// Arguments   : NumDoc   -->
		// Arguments   : Atributo -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public dynamic DaValorAtributoLock( string Filial,  string TipoDoc,  string Serie,  int Numdoc,  string Atributo)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributoLock( Filial,  TipoDoc,  Serie,  Numdoc,  Atributo);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributo", excep.Message);
			}
			return null;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributosIDLinhaLock
		// Description :
		// Arguments   : sID -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBE100.StdBECampos DaValorAtributosIDLinhaLock( string sID, params dynamic[] Atributos)
		{
			StdBE100.StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributosIDLinha", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosIDLinhaLock( sID,  tempParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributosIDLinha", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributosIDLock
		// Description :
		// Arguments   : Id -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBE100.StdBECampos DaValorAtributosIDLock( string Id, params dynamic[] Atributos)
		{
			StdBE100.StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributosID", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosIDLock( Id,  tempParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributosID", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributosLock
		// Description :
		// Arguments   : Filial  -->
		// Arguments   : TipoDoc -->
		// Arguments   : Serie   -->
		// Arguments   : NumDoc  -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBE100.StdBECampos DaValorAtributosLock( string Filial,  string TipoDoc,  string Serie,  int Numdoc, params dynamic[] Atributos)
		{
			StdBE100.StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributos", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosLock( Filial,  TipoDoc,  Serie,  Numdoc,  tempParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributos", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_IVndBSVendas_DevolveTextoAssinaturaDoc
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : clsDocumentoVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public string DevolveTextoAssinaturaDoc(string TipoDoc, string Serie, int Numdoc, string Filial)
		{


			return CertificacaoSoftware.DevolveTextoAssinaturaDoc(ref TipoDoc, ref Serie, ref Numdoc, ref Filial);



			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_DevolveTextoAssinaturaDoc", Information.Err().Description);

			return "";
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DevolveTextoAssinaturaDocID
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : Id -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public string DevolveTextoAssinaturaDocID(string Id)
		{
			string result = "";
			StdBE100.StdBECampos objCampos = null;
			string strTipoDoc = "";
			string strFilial = "";
			int lngNumDoc = 0;
			string strSerie = "";

			try
			{

				dynamic[] tempRefParam = new dynamic[]{"TipoDoc", "Serie", "NumDoc", "Filial"};
				objCampos = m_objErpBSO.Vendas.Documentos.DaValorAtributosID(Id, tempRefParam);

				if (objCampos != null)
				{

					//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					string tempRefParam2 = "TipoDoc";
					strTipoDoc = ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam2).Valor);
					//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					string tempRefParam3 = "Serie";
					strSerie = ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam3).Valor);
					//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to int. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					string tempRefParam4 = "NumDoc";
					lngNumDoc = ReflectionHelper.GetPrimitiveValue<int>(objCampos.GetItem(ref tempRefParam4).Valor);
					//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					string tempRefParam5 = "Filial";
					strFilial = ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam5).Valor);

					result = DevolveTextoAssinaturaDoc(strTipoDoc, strSerie, lngNumDoc, strFilial);

				}
				else
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IVndBSVendas_DevolveTextoAssinaturaDocID", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15788, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));

				}

				objCampos = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_DevolveTextoAssinaturaDocID", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DocumentoAnulado
		// Description :
		// Arguments   : Filial   -->
		// Arguments   : TipoDoc  -->
		// Arguments   : strSerie -->
		// Arguments   : NumDoc   -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool DocumentoAnulado(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{

			string tempRefParam = "Id";
			string strIdDoc = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.Vendas.Documentos.DaValorAtributo(Filial, TipoDoc, strSerie, Numdoc, tempRefParam));
			return m_objErpBSO.Vendas.Documentos.DocumentoAnuladoID(strIdDoc);

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DocumentoAnuladoID
		// Description :
		// Arguments   : Id -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool DocumentoAnuladoID(string Id)
		{

			return m_objErpBSO.DSO.Vendas.Documentos.DocumentoAnuladoID(Id);

		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_DocumentoCertificado
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		public bool DocumentoCertificado(string Id)
		{

			try
			{

				string tempRefParam = "Assinatura";

				return Strings.Len(ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributoID(ref Id, ref tempRefParam))) > 0;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_DocumentoCertificado", excep.Message);
			}
			return false;
		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_DocumentoTrataTransacaoEletronica
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		public bool DocumentoTrataTransacaoEletronicaEX(int TipoDocumento, string TipoDoc, string TipoEntidade, string Entidade, string IdDocB2B)
		{

			return FuncoesComuns100.FuncoesBS.Documentos.DocumentoTrataTransacaoEletronica(ConstantesPrimavera100.Modulos.Vendas, TipoDocumento, TipoDoc, TipoEntidade, Entidade, IdDocB2B);

		}

		public bool DocumentoTrataTransacaoEletronica(int TipoDocumento, string TipoDoc, string TipoEntidade, string Entidade)
		{

			return FuncoesComuns100.FuncoesBS.Documentos.DocumentoTrataTransacaoEletronica(ConstantesPrimavera100.Modulos.Vendas, TipoDocumento, TipoDoc, TipoEntidade, Entidade, "");

		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_Edita
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		public VndBEDocumentoVenda Edita(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{
			VndBEDocumentoVenda result = null;
			string strID = "";

			try
			{

				//UPGRADE_WARNING: (1068) m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
				string tempRefParam = "Id";
				strID = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributo(ref Filial, ref TipoDoc, ref strSerie, ref Numdoc, ref tempRefParam));

				if (Strings.Len(strID) > 0)
				{
					result = EditaID(strID);
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Edita", excep.Message);
			}

			return result;
		}

		public VndBEDocumentoVenda EditaID(string Id)
		{
			VndBEDocumentoVenda result = null;
			VndBE100.VndBEDocumentoVenda clsDocumentoVenda = null;
			StdBE100.StdBECampos objCampos = null;
			string strSQL = "";

			try
			{

				clsDocumentoVenda = m_objErpBSO.DSO.Vendas.Documentos.EditaID(ref Id);
				if (clsDocumentoVenda != null)
				{

					//BID: 576188 - CS.3185

					string switchVar = clsDocumentoVenda.TipoEntidade;
					if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
					{

						objCampos = m_objErpBSO.Base.Clientes.DaValorAtributos(clsDocumentoVenda.Entidade, "EfectuaRetencao", "SujeitoRecargo");
						string tempRefParam = "EfectuaRetencao";
						clsDocumentoVenda.SujeitoRetencao = (ReflectionHelper.GetPrimitiveValue<int>(objCampos.GetItem(ref tempRefParam).Valor) & Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencao)) != 0;
						string tempRefParam2 = "SujeitoRecargo";
						clsDocumentoVenda.SujeitoRecargo = ReflectionHelper.GetPrimitiveValue<bool>(objCampos.GetItem(ref tempRefParam2).Valor);
						objCampos = null;

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
					{ 

						objCampos = m_objErpBSO.Base.OutrosTerceiros.DaValorAtributos(clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidade, "EfectuaRetencao", "SujeitoRecargo");
						string tempRefParam3 = "EfectuaRetencao";
						clsDocumentoVenda.SujeitoRetencao = (ReflectionHelper.GetPrimitiveValue<int>(objCampos.GetItem(ref tempRefParam3).Valor) & Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencao)) != 0;
						string tempRefParam4 = "SujeitoRecargo";
						clsDocumentoVenda.SujeitoRecargo = ReflectionHelper.GetPrimitiveValue<bool>(objCampos.GetItem(ref tempRefParam4).Valor);
						objCampos = null;

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.EntidadeExterna)
					{ 

						clsDocumentoVenda.SujeitoRetencao = false;
						clsDocumentoVenda.SujeitoRecargo = false;

					}

					//BID: 576188 - CS.3185
					CalculaTotaisDocumento(ref clsDocumentoVenda);
					PreencheReservasLinhas(clsDocumentoVenda);

					foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha in clsDocumentoVenda.Linhas)
					{

						FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.PreencheCamposStkOrig(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda, ObjLinha);

					}

					//Na edição marcamos como atualizado
					strSQL = "UPDATE CabecDoc SET Desatualizado = 0 WHERE Id = '@1@' AND Desatualizado = 1";
					dynamic[] tempRefParam5 = new dynamic[]{clsDocumentoVenda.ID};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam5);
					DbCommand TempCommand = null;
					TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
					UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
					TempCommand.CommandText = strSQL;
					UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
					TempCommand.ExecuteNonQuery();


				}


				return clsDocumentoVenda;
			}
			catch (System.Exception excep)
			{
				result = clsDocumentoVenda;
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.EditaID", excep.Message);
			}
			return result;
		}


		private void IniciaTransaccao(ref bool blnIniciada)
		{
			if (!blnIniciada)
			{
				m_objErpBSO.IniciaTransaccao();
				blnIniciada = true;
			}
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : TrataIdTTE
		// Description : BID:578323
		// Arguments   : clsDocumentoVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void TrataIdTTE(VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{
			int intEstado = 0;
			int intNotificacao = 0;

			string strIdDocB2B = clsDocumentoVenda.IDDocB2B;

			//Retira os dados da TTE
			if (Strings.Len(strIdDocB2B) > 0 && clsDocumentoVenda.B2BTrataTrans)
			{

				if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha)
				{
					//Devido ao SII vai gerar sempre transação quando é gravado o documento.
					clsDocumentoVenda.IDDocB2B = "";
				}
				else
				{
					intEstado = m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.TransaccoesElectronicas.B2BTransaccoes.DaValorAtributo(strIdDocB2B, "Estado"));
					intNotificacao = m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.TransaccoesElectronicas.B2BTransaccoes.DaValorAtributo(strIdDocB2B, "UltEstadoRec"));
				}

			}

			//Se Estado: TTEB2BEstadoTransaccao.Enviado e TTEB2BStatusNotificacao.OcorreuErro ou Estado: TTEB2BEstadoTransaccao.RegistadoPEnviar e TTEB2BTipoTransaccao.Envio
			//BID 590179 : Estava "Or (intEstado = 1 And intNotificacao = 1)"
			if ((intEstado == 3 && intNotificacao == 1) || (intEstado == 1 && intNotificacao == 0))
			{

				clsDocumentoVenda.IDDocB2B = "";

			}

		}

		public void Actualiza(VndBEDocumentoVenda clsDocumentoVenda, string strAvisos, string IdDocLiqRet, string IdDocLiqRetGar)
		{
			VndBE100.VndBETabVenda clsTabVenda = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda = null;
			string StrErro = "";
			int NumPrest = 0;
			int intMult = 0;
			bool blnIniciouTrans = false;
			string FilialLiq = "";
			string strSerieLiq = "";
			int NumDocLiq = 0;
			string TipoDocLiq = "";
			int NumLinhaStkGerada = 0;
			dynamic ObjADC = null;
			int NumDocAntigo = 0;
			BasBESerie objSerie = null;
			StdBE100.StdBESpXml objXML = null;
			StdBE100.StdBELista objAvisosErros = null;
			VndBE100.VndBEDocumentoVenda clsDocVendaBD = null; //FIL
			int lngPos = 0;
			string strTipoAviso = "";
			bool blnExisteDocLiquidacao = false;
			double dblValorTotal = 0;
			double dblValorNovoPendente = 0;
			double dblPendente = 0;
			double[] dblValorPendente = null;
			string[] strIdHistorico = null;
			StdBE100.StdBECampos objBECampos = null;
			int intLinhaHistorico = 0;
			string strSQL = ""; //BID:536153
			FuncoesComuns100.clsBSPrestacoes objPrestacoes = null;
			string strID = "";
			string strIDLinha = "";
			//TTE
			StdBE100.StdBECampos objCamposTTE = null;
			bool blnDocTrataTransacao = false;
			bool blnRemoveuCessao = false;
			dynamic objLiquidacao = null; //CS.3405 - Adiantamentos em CC
			string TipoDocRegAnterior = "";
			string SerieDocRegAnterior = "";
			string FilialDocRegAnterior = "";
			int NumDocRegAnterior = 0;
			bool blnExistemAdiantamentos = false;
			int lngNumDocLiqAnterior = 0; //BID: 586141
			VndBE100.VndBELinhaDocumentoVenda[] arrLinhasAdiantamento = null;
			bool blnEmModoEdicao = false;
			string strAvisoCargaDescarga = ""; //BID 586829
			StdBE100.IStdBECallback callback = null;
			StdBE100.StdBELista ObjListaLiqAuto = null;
			//BID 588668
			bool blnExistemLinhasTrans = false;
			StdBE100.StdBELista objListaLinhasTransSoma = null;
			StdBE100.StdBELista objListaLinhasTrans = null;
			double dblDiferenca = 0;
			dynamic objOrigens = null;
			string strEntidadeTTE = "";
			string strTipoEntidadeTTE = "";
			BasBETipos.LOGTipoDocumento intTipoDocumentoTTE = BasBETipos.LOGTipoDocumento.LOGDocPedidoCotacao;
			string strIdLinhasFechadas = "";
			//FIM 599046

			try
			{

				//Bug 8013
				blnExisteDocLiquidacao = false;

				//Utilizado qundo mo documento tem adiantamentos de conta corrente.
				dblValorPendente = ArraysHelper.RedimPreserve(dblValorPendente, new int[]{1});
				strIdHistorico = ArraysHelper.RedimPreserve(strIdHistorico, new int[]{1});



				//Preenche dados da contabilidade
				string tempRefParam = ConstantesPrimavera100.Modulos.Vendas;
				FuncoesComuns100.FuncoesBS.Documentos.PreencheDadosCBL(ref tempRefParam, clsDocumentoVenda);

				//Preenche alguns dados necessários para efetuar a atualização
				PreencheDadosActualiza(ref clsDocumentoVenda, ref clsTabVenda, ref objSerie, ref strSerieLiq, ref intMult);

				BasBECargaDescarga withVar = null;

				// Se o documento era do tipo rascunho então é necessario alterar o estado da prop EmModoEdicao
				// para false para se poder gravar, pois só assim é considerado novo.
				if (clsDocumentoVenda.AntRascunho)
				{

					clsDocumentoVenda.EmModoEdicao = false;

				}

				// Tratamento do LOG
				bool tempRefParam2 = !clsDocumentoVenda.EmModoEdicao;
				m_objErpBSO.DSO.Vendas.Documentos.set_LogActivo(ref tempRefParam2);

				//Gera Pendente por Linha
				objPrestacoes = FuncoesComuns100.FuncoesBS.Instancia_Prestacoes;
				objPrestacoes.Modulo = ConstantesPrimavera100.Modulos.Vendas;

				//BID 546594 - Só faz sentido calcular as prestações se o documento efectuar ligação a contas correntes
				if (clsTabVenda.LigacaoCC)
				{

					objPrestacoes.CalculaPrestacoes(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda);

				}
				//^CR.1103

				//TTE - Verifica se este documento vai tratar transações eletrónicas
				if (Strings.Len(clsDocumentoVenda.Entidade) == 0)
				{ //BID 594302
					blnDocTrataTransacao = false;
				}
				else
				{
					//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
					string tempRefParam3 = clsDocumentoVenda.Tipodoc;
					string tempRefParam4 = "TipoDocumento";
					intTipoDocumentoTTE = (BasBETipos.LOGTipoDocumento) m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam3, tempRefParam4));

					if (intTipoDocumentoTTE == BasBETipos.LOGTipoDocumento.LOGDocFinanceiro || intTipoDocumentoTTE == BasBETipos.LOGTipoDocumento.LOGDocStk_Transporte)
					{
						strTipoEntidadeTTE = clsDocumentoVenda.TipoEntidadeFac;
						strEntidadeTTE = clsDocumentoVenda.EntidadeFac;
					}
					else
					{
						strEntidadeTTE = clsDocumentoVenda.Entidade;
						strTipoEntidadeTTE = clsDocumentoVenda.TipoEntidade;
					}
					blnDocTrataTransacao = FuncoesComuns100.FuncoesBS.Documentos.DocumentoTrataTransacaoEletronica(ConstantesPrimavera100.Modulos.Vendas, clsTabVenda.TipoDocumento, clsDocumentoVenda.Tipodoc, strTipoEntidadeTTE, strEntidadeTTE, clsDocumentoVenda.IDDocB2B);
				}

				//-> 1º INICIO DA TRANSACÇÃO

				blnIniciouTrans = false;
				IniciaTransaccao(ref blnIniciouTrans);

				//Se está em modo de edição, remove-se a liquidação anterior
				if (clsDocumentoVenda.EmModoEdicao)
				{

					FuncoesComuns100.FuncoesBS.Documentos.RemoveDocCompensacao(clsDocumentoVenda, ref TipoDocRegAnterior, ref SerieDocRegAnterior, ref FilialDocRegAnterior, ref NumDocRegAnterior);

				}
				else
				{

					//BID 597011
					if (clsTabVenda.SAFTTipoDoc == ConstantesPrimavera100.Fiscal.CodigoFacturaSimplificada)
					{

						withVar = clsDocumentoVenda.CargaDescarga;

						//Limpar os campos da carga
						withVar.DataCarga = "";
						withVar.HoraCarga = "";
						withVar.LocalCarga = "";
						withVar.MoradaCarga = "";
						withVar.Morada2Carga = "";
						withVar.LocalidadeCarga = "";
						withVar.CodPostalCarga = "";
						withVar.CodPostalLocalidadeCarga = "";
						withVar.DistritoCarga = "";
						withVar.PaisCarga = "";

						//Limpar os campos da descarga
						withVar.DataDescarga = "";
						withVar.HoraDescarga = "";
						withVar.LocalDescarga = "";
						withVar.MoradaEntrega = "";
						withVar.Morada2Entrega = "";
						withVar.LocalidadeEntrega = "";
						withVar.CodPostalEntrega = "";
						withVar.CodPostalLocalidadeEntrega = "";
						withVar.DistritoEntrega = "";
						withVar.PaisEntrega = "";
						//
						withVar.TipoEntidadeEntrega = "";
						withVar.EntidadeDescarga = "";
						withVar.EntidadeEntrega = "";
						withVar.NomeEntrega = "";
						withVar.UsaMoradaAlternativaEntrega = false;
						withVar.MoradaAlternativaEntrega = "";
						withVar.Matricula = "";


					}

				}

				//Bloqueia registos
				BloqueiaRegistosDocumentoVenda(clsDocumentoVenda);

				if (!clsDocumentoVenda.EmModoEdicao)
				{

					clsDocumentoVenda.Tipodoc = clsDocumentoVenda.Tipodoc.ToUpper();

					if (clsDocumentoVenda.Filial == m_objErpBSO.Base.Filiais.CodigoFilial)
					{

						NumDocAntigo = clsDocumentoVenda.NumDoc;
						//BID 17364
						//O objeto "objSerie" está a ser editado fora da transação, o numerador pode estar desatualizado
						clsDocumentoVenda.NumDoc = m_objErpBSO.Base.Series.ProximoNumero(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie);

					}

				}
				else
				{

					//Se o documento efectuar liquidação automática necessito da chave do documento de liquidacao
					if (clsTabVenda.LiquidacaoAutomatica && clsTabVenda.LigacaoCC)
					{

						strSQL = "SELECT Filial, Serie, TipoDoc, NumDoc FROM LinhasLiq WHERE FilialOrig = '@1@' AND ModuloOrig = '@2@' AND TipoDocOrig = '@3@' AND SerieOrig = '@4@' AND NumDocOrigInt = @5@";
						dynamic[] tempRefParam5 = new dynamic[]{clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam5);
						ObjListaLiqAuto = m_objErpBSO.Consulta(strSQL);

						if (!ObjListaLiqAuto.NoFim())
						{

							blnExisteDocLiquidacao = true;
							FilialLiq = m_objErpBSO.DSO.Plat.Utils.FStr(ObjListaLiqAuto.Valor("Filial")); //PriGlobal: IGNORE
							TipoDocLiq = m_objErpBSO.DSO.Plat.Utils.FStr(ObjListaLiqAuto.Valor("TipoDoc")); //PriGlobal: IGNORE
							strSerieLiq = m_objErpBSO.DSO.Plat.Utils.FStr(ObjListaLiqAuto.Valor("Serie")); //PriGlobal: IGNORE
							NumDocLiq = m_objErpBSO.DSO.Plat.Utils.FLng(ObjListaLiqAuto.Valor("NumDoc")); //PriGlobal: IGNORE

						}
						else
						{

							blnExisteDocLiquidacao = false;

						}

						ObjListaLiqAuto = null;

					}

					//FIL
					//Verifica remoção de linhas
					if (m_objErpBSO.Base.Filiais.LicencaDeFilial)
					{

						string tempRefParam6 = clsDocumentoVenda.ID;
						clsDocVendaBD = EditaID(tempRefParam6);

						//-> 2º INICIO DA TRANSACÇÃO

						IniciaTransaccao(ref blnIniciouTrans);
						RegistaLinhasRemovidas(clsDocumentoVenda.Linhas, clsDocVendaBD.Linhas);

						clsDocVendaBD = null;

					}

					//FIL
					if (m_objErpBSO.Base.Filiais.LicencaDeFilial)
					{

						if (!clsTabVenda.PermiteAltAposExp)
						{

							if (ReflectionHelper.IsGreaterThan(m_objErpBSO.DSO.Base.Filiais.TSUltimaExportacao("CabecDoc"), m_objErpBSO.DSO.Base.Filiais.DaAtributoTimesTamp("SELECT VersaoUltAct As Versao FROM CabecDoc WHERE Filial='" + clsDocumentoVenda.Filial + "' AND Serie='" + clsDocumentoVenda.Serie + "' AND TipoDoc='" + clsDocumentoVenda.Tipodoc + "' AND NumDoc=" + clsDocumentoVenda.NumDoc.ToString())))
							{ //PriGlobal: IGNORE

								string tempRefParam7 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9070, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam8 = new dynamic[]{clsDocumentoVenda.Tipodoc};
								StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.Actualiza", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam7, tempRefParam8));

							}

						}

					}

				}

				//-> 3º INICIO DA TRANSACÇÃO
				IniciaTransaccao(ref blnIniciouTrans);

				// Valida se o documento está em conformidade
				if (ValidaActualizacao(clsDocumentoVenda, clsTabVenda, strSerieLiq, StrErro, objSerie, blnDocTrataTransacao, strAvisos))
				{

					//Abrir as linhas de ECLS fechadas
					int tempForVar = clsDocumentoVenda.Linhas.Removidas.NumItens;
					for (int lngLinha = 1; lngLinha <= tempForVar; lngLinha++)
					{

						string tempRefParam9 = "";
						string tempRefParam10 = clsDocumentoVenda.Linhas.Removidas.GetEdita(lngLinha);
						AbreLinhasECLFechadasAnulacao(ref tempRefParam9, ref tempRefParam10);

					}


					//CR.781
					if (Convert.ToBoolean(m_objErpBSO.Inventario.Params.ReactivaLote))
					{

						ReactivaLotes(clsDocumentoVenda, clsTabVenda);
					}
					//^CR.781

					m_objErpBSO.Inventario.Encargos.Remove(clsDocumentoVenda.ID);

					objXML = new StdBE100.StdBESpXml();
					objXML.Inicializa();

					//** Actualiza o cabeçalho do documento de venda
					AplicaMultiplicador(intMult, clsDocumentoVenda, true, false);

					//** Verifica se o documento de venda tem ligação às contas correntes
					if (clsTabVenda.LigacaoCC)
					{

						NumPrest = clsDocumentoVenda.Prestacoes.NumItens;

						//Se o documento não tem prestações
						if (NumPrest == 0)
						{

							NumPrest = 1;
							strIDLinha = "";
							objPrestacoes.ResumoRetencoes = clsDocumentoVenda.Retencoes;
						}

						for (lngPos = 1; lngPos <= NumPrest; lngPos++)
						{

							if (clsDocumentoVenda.Prestacoes.NumItens > 0)
							{


								switch(Convert.ToInt32(Double.Parse(clsDocumentoVenda.Prestacoes.GetEdita(lngPos).TipoLinha)))
								{
									case 30 : case 40 : case 41 : case 50 : case 51 : case 70 : case 80 : case 90 : case 60 : case 65 : 
										//Neste caso não faz nada 
										 
										break;
									default:
										if (clsDocumentoVenda.Prestacoes.NumItens > 0)
										{

											strIDLinha = clsDocumentoVenda.Prestacoes.GetEdita(lngPos).IdLinhaOrigem;

										} 
										 
										//BID: 583062 
										if (clsDocumentoVenda.EmModoEdicao)
										{

											strID = Convert.ToString(m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributoIDDoc(clsDocumentoVenda.ID, lngPos, "Id"));
											if (Strings.Len(strID) == 0)
											{

												bool tempRefParam11 = true;
												strID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam11);

											}

										}
										else
										{

											bool tempRefParam12 = true;
											strID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam12);

										} 
										 
										objPrestacoes.AtribuiIdHistorico(strIDLinha, strID); 
										 
										//Se for Pagamento por prestações então não guarda o Id da linha origem 
										if (clsDocumentoVenda.Prestacoes != null)
										{

											if (objPrestacoes.TipoCondicaoPgt == "4")
											{

												if (clsDocumentoVenda.Prestacoes.NumItens > 0)
												{

													clsDocumentoVenda.Prestacoes.GetEdita(lngPos).IdLinhaOrigem = "";

												}

											}

										} 
										 
										PreencheXMLHistorico(clsDocumentoVenda, clsTabVenda, lngPos, objXML, strID); 
										 
										break;
								}

							}
							else
							{

								//BID: 583062
								if (clsDocumentoVenda.EmModoEdicao)
								{

									strID = Convert.ToString(m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributoIDDoc(clsDocumentoVenda.ID, 1, "Id"));
									if (Strings.Len(strID) == 0)
									{

										bool tempRefParam13 = true;
										strID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam13);

									}

								}
								else
								{

									bool tempRefParam14 = true;
									strID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam14);

								}

								objPrestacoes.AtribuiIdHistorico(strIDLinha, strID);
								PreencheXMLHistorico(clsDocumentoVenda, clsTabVenda, lngPos, objXML, strID);

							}

						}

					}

					if (clsDocumentoVenda.Prestacoes.NumItens > 0)
					{

						clsDocumentoVenda.Retencoes = objPrestacoes.ResumoRetencoes;

					}


					if (Strings.Len(clsDocumentoVenda.IDEstorno) > 0)
					{

						DbCommand TempCommand = null;
						TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
						UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
						TempCommand.CommandText = "UPDATE CabecDocStatus SET Estado='" + ConstantesPrimavera100.Documentos.EstadoDocTransformado + "' WHERE IdCabecDoc='" + clsDocumentoVenda.IDEstorno + "'";
						UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
						TempCommand.ExecuteNonQuery();

					}

					lngPos = 1;
					NumLinhaStkGerada = 1;

					blnExistemAdiantamentos = false;
					blnExistemLinhasTrans = false; //BID 588668

					//** Actualiza o cabeçalho do documento de venda
					AplicaMultiplicador(intMult, clsDocumentoVenda, false, true);

					//LINHAS
					foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda2 in clsDocumentoVenda.Linhas)
					{
						ClsLinhaVenda = ClsLinhaVenda2;

						//Controlar a existência de adiantamentos no documento
						blnExistemAdiantamentos = blnExistemAdiantamentos || (ClsLinhaVenda.TipoLinha == "90" && (Strings.Len(ClsLinhaVenda.IDHistorico) > 0) || (Strings.Len(ClsLinhaVenda.DadosAdiantamento.IDHistorico) > 0));

						blnExistemLinhasTrans = blnExistemLinhasTrans || Strings.Len(ClsLinhaVenda.IDLinhaOriginal) > 0; //BID 588668

						//** Actualiza as linhas do documento de venda
						if (clsTabVenda.LigacaoStocks && ClsLinhaVenda.MovStock == "S")
						{

							ClsLinhaVenda.NumLinhaStkGerada = NumLinhaStkGerada;
							NumLinhaStkGerada++;

						}

						ClsLinhaVenda.RegimeIva = clsDocumentoVenda.RegimeIva;

						CalculaEstadoLinha(ClsLinhaVenda);

						if (clsDocumentoVenda.EmModoEdicao)
						{

							//Se documento está em modo de edição então guarda o valor do adiantamento
							//Este valor será utilizado para calulo do valor do novo pendente.
							if (ClsLinhaVenda.TipoLinha == "90" && Strings.Len(ClsLinhaVenda.IDHistorico) > 0)
							{

								dblValorPendente = ArraysHelper.RedimPreserve(dblValorPendente, new int[]{intLinhaHistorico + 1});
								strIdHistorico = ArraysHelper.RedimPreserve(strIdHistorico, new int[]{intLinhaHistorico + 1});

								string tempRefParam15 = ClsLinhaVenda.IdLinha;
								dynamic[] tempRefParam16 = new dynamic[]{"TotalIliquido", "TotalIva"};
								objBECampos = m_objErpBSO.Vendas.Documentos.DaValorAtributosIDLinha(tempRefParam15, tempRefParam16);

								if (objBECampos != null)
								{
									//UPGRADE-WARNING: It was not possible to determine if this addition expression should use an addition operator (+) or a concatenation operator (&)
									string tempRefParam17 = "TotalIliquido";
									string tempRefParam18 = "TotalIva";
									dblValorPendente[intLinhaHistorico] = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(Convert.ToDouble(ReflectionHelper.GetPrimitiveValue<double>(objBECampos.GetItem(ref tempRefParam17).Valor) + ReflectionHelper.GetPrimitiveValue<double>(objBECampos.GetItem(ref tempRefParam18).Valor)), 2);
								}
								strIdHistorico[intLinhaHistorico] = ClsLinhaVenda.IDHistorico;

								intLinhaHistorico++;

							}

						}

						//Extensibilidade Orçamental - BID: 592942 e 592113
						if (ClsLinhaVenda.ProcessoCBL == FuncoesComuns100.FuncoesBS.ProcessosExec.TokenNovoProcessoExecucao)
						{
							ClsLinhaVenda.ProcessoCBL = FuncoesComuns100.FuncoesBS.ProcessosExec.DaCodProcesso(clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc.ToString());
						}


						//Transição do estado do projecto por linha
						//BID 594209
						if (Convert.ToBoolean(m_objErpBSO.Projectos.Licenca.Projectos.Base))
						{

							if (Strings.Len(clsDocumentoVenda.IDObra) == 0)
							{

								if (Strings.Len(ClsLinhaVenda.IDObra) > 0 && !clsDocumentoVenda.EmModoEdicao)
								{

									m_objErpBSO.Projectos.Projectos.TransitaEstadoId(ClsLinhaVenda.IDObra, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc);

								}

							}

						}
						//^BID 594209

						lngPos++;

						//Se a linha ou o documento foram fechados, e existem reservas, vamos atualizar as reservas
						if ((ClsLinhaVenda.Fechado || clsDocumentoVenda.Fechado) && ClsLinhaVenda.ReservaStock.Linhas.NumItens > 0 && clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
						{

							strIdLinhasFechadas = strIdLinhasFechadas + "'" + ClsLinhaVenda.IdLinha + "',";

						}

						ClsLinhaVenda = null;
					}


					//CS.1369
					//Gera assinatura apenas para empresas com sede em Portugal
					CertificacaoSoftware.AssinaDocumento(clsDocumentoVenda, clsTabVenda, objSerie);

					//BID 15998
					if (Strings.Len(clsDocumentoVenda.CertificadoRecuperacao) > 40 && FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
					{
						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VndBSVendas.Actualiza", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18609, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
					}
					//^BID 15998

					//TODO: RED2REMOVER
					//IC - De momento não existe ainda alternativa nos Docs Internos para isto.

					//CS.2975 - Anula documento emitos para acompanhar bens em circulação
					//            If Not clsDocumentoVenda.EmModoEdicao Then
					//
					//                If LenB(clsDocumentoVenda.IdDocOrigem) > 0 Then
					//
					//                    If clsDocumentoVenda.ModuloOrigem = ConstantesPrimavera100.Modulos.Stocks Then
					//
					//                        m_objErpBSO.Inventario.Stocks.AnulaDocumentosBensCirculacao clsDocumentoVenda.IdDocOrigem, strAvisos
					//
					//                    End If
					//
					//                End If
					//
					//            End If
					//^CS.1369

					//TTE - Trata da transação eletrónica
					clsDocumentoVenda.B2BTrataTrans = blnDocTrataTransacao;

					//CS.3873
					TrataPayTransacao(clsDocumentoVenda, ref strAvisos); //BID 590093
					//CS.3873 END

					//BID:578323
					TrataIdTTE(clsDocumentoVenda);

					//Factoring - Remove o documento, caso esteja numa cessao
					if (clsDocumentoVenda.EmModoEdicao && FuncoesComuns100.FuncoesBS.Utils.FactoringDisponivel())
					{

						blnRemoveuCessao = Convert.ToBoolean(m_objErpBSO.Factoring.Cessoes.RemoveDocumentoCessao(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Filial, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc));

					}

					//BID 586829
					if (!FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
					{

						if (!FuncoesComuns100.FuncoesBS.Documentos.CargaAntesDaDescarga(clsDocumentoVenda, clsTabVenda, ref strAvisoCargaDescarga))
						{

							strAvisos = strAvisos + strAvisoCargaDescarga + Environment.NewLine;

						}

					}
					//Fim 586829

					//Tratamento Reservas
					if (clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
					{

						RemoveReservasCanceladas(clsDocumentoVenda);

						ActualizaReservas(clsDocumentoVenda);

					}

					//Integração com os Stocks
					if (clsTabVenda.LigacaoStocks || clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
					{

						objOrigens = FuncoesComuns100.FuncoesBS.Documentos.PreencheBEOrigens(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda, clsTabVenda);
						m_objErpBSO.Inventario.Documentos.Actualiza(objOrigens, strAvisos);

					}

					string tempRefParam19 = objXML.ValorXml;
					objAvisosErros = m_objErpBSO.DSO.Vendas.Documentos.ActualizaDocumento(ref clsDocumentoVenda, ref tempRefParam19);

					//Nas encomendas, se estamos a fechar linhas ou o documento, vamos atualizar as reservas
					if (clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
					{

						//Fechar as linhas
						if (Strings.Len(strIdLinhasFechadas) > 0)
						{

							TrataReservasFechoLinha(strIdLinhasFechadas.Substring(0, Math.Min(Strings.Len(strIdLinhasFechadas) - 1, strIdLinhasFechadas.Length)));

						}

					}

					//Actualiza o numerador dos números de série
					FuncoesComuns100.FuncoesBS.Documentos.AtualizaContadorNumeroSerie(clsDocumentoVenda.Linhas);

					//BID 588668
					if (blnExistemLinhasTrans)
					{

						//Query que permite verificar se existem linhas no documento origem com quantidade transformada superior à quantidade da linha
						strSQL = "SELECT DISTINCT lds.IdLinhasDoc, (lds.QuantTrans-lds.Quantidade) AS Diferenca FROM LinhasDoc ld " + 
						         "INNER JOIN LinhasDocTrans ldt ON ldt.IdLinhasDoc=ld.Id INNER JOIN LinhasDocStatus lds ON lds.IdLinhasDoc=ldt.IdLinhasDocOrigem " + 
						         "WHERE ld.IdCabecDoc='" + clsDocumentoVenda.ID + "' AND ABS(lds.QuantTrans) > ABS(lds.quantidade)";
						objListaLinhasTransSoma = m_objErpBSO.Consulta(strSQL);

						while (!objListaLinhasTransSoma.NoFim())
						{

							dblDiferenca = Double.Parse(objListaLinhasTransSoma.Valor("Diferenca")); //PriGlobal: IGNORE

							//Query que permite listar todas as linhas que transformaram uma determinada linha do documento origem
							strSQL = "SELECT IdLinhasDoc,QuantTrans FROM LinhasDocTrans WHERE IdLinhasDocOrigem='" + objListaLinhasTransSoma.Valor("IdLinhasDoc") + "' ORDER BY VersaoUltAct DESC";
							objListaLinhasTrans = m_objErpBSO.Consulta(strSQL);

							while (!objListaLinhasTrans.NoFim() && dblDiferenca != 0)
							{

								if (Math.Abs(Double.Parse(objListaLinhasTrans.Valor("QuantTrans"))) >= Math.Abs(dblDiferenca))
								{ //PriGlobal: IGNORE

									//A diferença entre a quantidade transformada e a quantidade da linha pode ser distribuida numa única linha
									DbCommand TempCommand_2 = null;
									TempCommand_2 = m_objErpBSO.DSO.BDAPL.CreateCommand();
									UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_2);
									TempCommand_2.CommandText = "UPDATE LinhasDocTrans SET QuantTrans = QuantTrans - " + m_objErpBSO.DSO.Plat.Sql.ConverteNumeroSQL(dblDiferenca) + " WHERE IdLinhasDoc = '" + objListaLinhasTrans.Valor("IdLinhasDoc") + "'";
									UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_2);
									TempCommand_2.ExecuteNonQuery();
									dblDiferenca = 0;

								}
								else
								{

									//A diferença entre a quantidade transformada e a quantidade da linha deve ser distribuida em mais linha(s)
									DbCommand TempCommand_3 = null;
									TempCommand_3 = m_objErpBSO.DSO.BDAPL.CreateCommand();
									UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_3);
									TempCommand_3.CommandText = "UPDATE LinhasDocTrans SET QuantTrans = 0 WHERE IdLinhasDoc = '" + objListaLinhasTrans.Valor("IdLinhasDoc") + "'";
									UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_3);
									TempCommand_3.ExecuteNonQuery();
									dblDiferenca -= Double.Parse(objListaLinhasTrans.Valor("QuantTrans")); //PriGlobal: IGNORE

								}

								objListaLinhasTrans.Seguinte();

							}

							objListaLinhasTrans = null;

							objListaLinhasTransSoma.Seguinte();

						}

						objListaLinhasTransSoma = null;

					}
					//Fim 588668

					//Só executa o ciclo se realmente existirem adiantamentos
					if (blnExistemAdiantamentos)
					{

						//CR.630 Actualiza o valor dos adiantamentos.
						foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda3 in clsDocumentoVenda.Linhas)
						{
							ClsLinhaVenda = ClsLinhaVenda3;

							//- Elimina os adiantamentos seleccionados no documento de venda
							if (ClsLinhaVenda.TipoLinha == "90" && Strings.Len(ClsLinhaVenda.IDHistorico) > 0)
							{

								//Adiantamento seleccionado da C/C
								ObjADC = (dynamic) m_objErpBSO.PagamentosRecebimentos.Pendentes.EditaId(ClsLinhaVenda.IDHistorico);

								if (ObjADC != null)
								{

									//Valor do adiantamento por defeito é igual ao valor da tabela Pendentes
									dblPendente = Math.Abs(ObjADC.ValorPendente);

									for (intLinhaHistorico = 0; intLinhaHistorico <= strIdHistorico.GetUpperBound(0); intLinhaHistorico++)
									{

										if (strIdHistorico[intLinhaHistorico] == ClsLinhaVenda.IDHistorico)
										{

											//Calcula o valor do novo pendente
											dblPendente = Math.Abs(ObjADC.ValorPendente) + Math.Abs(dblValorPendente[intLinhaHistorico]);
										}
									}

									dblValorTotal = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ClsLinhaVenda.TotalIliquido + ClsLinhaVenda.TotalIva, 2);
									dblValorNovoPendente = Math.Abs(TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblPendente - dblValorTotal, 2));

									string tempRefParam20 = ObjADC.Tipodoc;
									if (FuncoesComuns100.FuncoesBS.Documentos.daGereLimiteCredito(ConstantesPrimavera100.Modulos.ContasCorrentes, ref tempRefParam20, ObjADC.TipoConta, ObjADC.Estado) && (!clsDocumentoVenda.EmModoEdicao))
									{

										dblValorTotal = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ClsLinhaVenda.TotalIliquido + ClsLinhaVenda.TotalIva, 2);
										dblValorNovoPendente = Math.Abs(TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ObjADC.ValorPendente - dblValorTotal, 2));

										if (FuncoesComuns100.FuncoesBS.Entidades.DaNaturezaEntidade(ObjADC.TipoEntidade, ObjADC.Entidade) == ConstantesPrimavera100.TiposEntidade.Natureza_Credor)
										{

											double tempRefParam21 = (-1 * dblValorTotal) * intMult;
											FuncoesComuns100.FuncoesBS.Entidades.ActualizaTotalDebitoEntidade(ObjADC.TipoEntidade, ObjADC.Entidade, ref tempRefParam21, ObjADC.Cambio, clsDocumentoVenda.CambioMBase, clsDocumentoVenda.CambioMAlt);
										}
										else
										{

											double tempRefParam22 = dblValorTotal * intMult;
											FuncoesComuns100.FuncoesBS.Entidades.ActualizaTotalDebitoEntidade(ObjADC.TipoEntidade, ObjADC.Entidade, ref tempRefParam22, ObjADC.Cambio, clsDocumentoVenda.CambioMBase, clsDocumentoVenda.CambioMAlt);
										}
									}

									//Se for uma linha de estorno, actualiza os pendentes..
									if (Strings.Len(ClsLinhaVenda.IdLinhaEstorno) > 0)
									{

										//verifica se existe nos pendentes..
										if (~Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Pendentes.ExisteId(ClsLinhaVenda.IDHistorico)) != 0)
										{

											//Se não existe, então evoca a SP para inserir novamente nos pendentes..
											strSQL = "EXEC [GCP_CCT_InserePendente] '@1@','@2@','@3@','@4@' ";
											dynamic[] tempRefParam23 = new dynamic[]{ClsLinhaVenda.IDHistorico, ClsLinhaVenda.IdLinhaEstorno, ConstantesPrimavera100.Modulos.ContasCorrentes, ConstantesPrimavera100.Modulos.Vendas};
											strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam23);

											DbCommand TempCommand_4 = null;
											TempCommand_4 = m_objErpBSO.DSO.BDAPL.CreateCommand();
											UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_4);
											TempCommand_4.CommandText = strSQL;
											UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_4);
											TempCommand_4.ExecuteNonQuery();
										}
										else
										{

											//se existe então actualiza o valor pendente...
											//Nota: ao actualizar o valor pendente, uma vez que é um estorno, deve incrementar o seu valor e não decrementar como está actualmente nas vendas...
											m_objErpBSO.PagamentosRecebimentos.Pendentes.ActualizaValorAtributo(ObjADC.Modulo, ObjADC.Tipodoc, ObjADC.NumDocInt, ObjADC.NumPrestacao, ObjADC.Serie, ObjADC.Filial, ObjADC.Estado, ObjADC.NumTransferencia, "ValorPendente", dblValorNovoPendente * -1);
										}
									}
									else
									{

										if (dblValorNovoPendente == 0)
										{

											//BID 569786
											//m_objErpBSO.PagamentosRecebimentos.Pendentes.RemovePendenteEstadoID ClsLinhaVenda.IDHistorico, ClsLinhaVenda.EstadoAdiantamento
											m_objErpBSO.PagamentosRecebimentos.Pendentes.RemovePendente(ObjADC.Filial, ObjADC.Modulo, ObjADC.Tipodoc, ObjADC.Serie, ObjADC.NumDocInt, ObjADC.Estado, ObjADC.NumTransferencia, ObjADC.NumPrestacao);
											//Fim 569786
										}
										else
										{

											m_objErpBSO.PagamentosRecebimentos.Pendentes.ActualizaValorAtributo(ObjADC.Modulo, ObjADC.Tipodoc, ObjADC.NumDocInt, ObjADC.NumPrestacao, ObjADC.Serie, ObjADC.Filial, ObjADC.Estado, ObjADC.NumTransferencia, "ValorPendente", dblValorNovoPendente * -1);
										}
									}

									ObjADC = null;
								}
							}
							ClsLinhaVenda = null;
						}

						//^CR.630

						//CS.3405 - Adiantamentos em CC
						objLiquidacao = FuncoesComuns100.FuncoesBS.Documentos.PreencheDocLiquidacao(clsDocumentoVenda, ConstantesPrimavera100.Modulos.Vendas, intMult, true, TipoDocRegAnterior, SerieDocRegAnterior, FilialDocRegAnterior, NumDocRegAnterior);

						//Grava o documento de liquidação
						if (objLiquidacao != null)
						{

							//BID 584681
							if (clsDocumentoVenda.TotalRetencao != 0 || clsDocumentoVenda.TotalRetencaoGarantia != 0)
							{

								int tempForVar2 = objLiquidacao.LinhasLiquidacao.NumItens;
								for (lngPos = 1; lngPos <= tempForVar2; lngPos++)
								{

									if (objLiquidacao.LinhasLiquidacao.GetEdita(lngPos).Retencoes != null)
									{

										objLiquidacao.LinhasLiquidacao.GetEdita(lngPos).Retencoes = null;

									}

								}

								if (Math.Abs(clsDocumentoVenda.TotalRetencao + clsDocumentoVenda.TotalRetencaoGarantia) > Math.Abs(clsDocumentoVenda.TotalDocumento))
								{

									StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15331, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

								}

							}
							//Fim 584681

							m_objErpBSO.PagamentosRecebimentos.Liquidacoes.Actualiza(objLiquidacao);
							//Actualiza o ID no cabeçalho do documento
							string tempRefParam24 = clsDocumentoVenda.ID;
							string tempRefParam25 = "IDRegularizacao";
							dynamic tempRefParam26 = objLiquidacao.ID;
							m_objErpBSO.Vendas.Documentos.ActualizaValorAtributoID(tempRefParam24, tempRefParam25, tempRefParam26); //PriGlobal: IGNORE

						}
					}

					if (objAvisosErros != null)
					{

						if (!objAvisosErros.Vazia())
						{

							objAvisosErros.Inicio();

							while (!objAvisosErros.NoFim())
							{

								strTipoAviso = objAvisosErros.Valor("Tipo"); //PriGlobal: IGNORE

								//Alteração de documentos em datas anteriores à última contagem
								if (StringsHelper.ToDoubleSafe(objAvisosErros.Valor("ID")) == 35001)
								{

									//BID 525566
									if (m_objErpBSO.Contexto.ObjUtilizador() == null)
									{

										strTipoAviso = "A";

									}
									else
									{

										//Fim 525566
										//BID 553975 (foi adicionado o teste "...PodeExecutarOperacao("mnuDocModificaInv"...")
										if ((Convert.ToInt32(m_objErpBSO.Contexto.ObjUtilizador().Administrador) | Convert.ToInt32(m_objErpBSO.Contexto.ObjUtilizador().PodeExecutarOperacao("mnuDocModificaInv", ConstantesPrimavera100.AbreviaturasApl.Inventario))) != 0)
										{ //PriGlobal: IGNORE

											strTipoAviso = "A";

										}
										else
										{

											strTipoAviso = "E";

										}

									} //BID 525566

								}

								//VP BID 546763
								if (StringsHelper.ToDoubleSafe(objAvisosErros.Valor("ID")) == 231009)
								{
									if (Convert.ToDouble(m_objErpBSO.Inventario.Params.ControloRupturaStock) == 1)
									{
										strTipoAviso = "A";
									}
								}

								if (strTipoAviso == "A")
								{ //PriGlobal: IGNORE

									strAvisos = strAvisos + objAvisosErros.Valor("ID") + " - " + objAvisosErros.Valor("Mensagem") + Environment.NewLine; //PriGlobal: IGNORE

								}
								else
								{

									StrErro = StrErro + objAvisosErros.Valor("ID") + " - " + objAvisosErros.Valor("Mensagem") + Environment.NewLine; //PriGlobal: IGNORE

								}

								objAvisosErros.Seguinte();

							}

						}

					}

					objAvisosErros = null;

					objXML.Termina();

					objXML = null;

					if (Strings.Len(StrErro) > 0)
					{

						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.Actualiza", StrErro);

					}

					//FIL
					//É necessário fazer o teste se é terminal
					if (clsDocumentoVenda.Filial == m_objErpBSO.Base.Filiais.CodigoFilial || (m_objErpBSO.Base.Filiais.ETerminal(clsDocumentoVenda.Filial) && clsDocumentoVenda.EmModoEdicao))
					{

						if (clsTabVenda.LigacaoCC)
						{

							//** Verificar se gera automaticamente o documento de liquidação e liquidar
							if (clsTabVenda.LiquidacaoAutomatica)
							{

								//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Series.DaValorAtributo() of type Variant is being forced to int. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
								lngNumDocLiqAnterior = ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.ContasCorrentes, clsTabVenda.DocumentoLiqAGerar, strSerieLiq, "Numerador")); //BID: 586141
								EfectuaLiquidacaoAutomatica(blnExisteDocLiquidacao, FilialLiq, NumDocLiq, strSerieLiq, clsTabVenda.DocumentoLiqAGerar, clsTabVenda.Estado, clsDocumentoVenda);

								//BID 595266 : foi adicionado o teste [And blnExisteDocLiquidacao]
								if (clsDocumentoVenda.EmModoEdicao && blnExisteDocLiquidacao && m_objErpBSO.DSO.Base.Series.ProximoNumero(ConstantesPrimavera100.Modulos.ContasCorrentes, clsTabVenda.DocumentoLiqAGerar, strSerieLiq, false) != NumDocLiq + 1)
								{ //Repôr o numerador se não é o último
									m_objErpBSO.Base.Series.ActualizaNumerador(ConstantesPrimavera100.Modulos.ContasCorrentes, clsTabVenda.DocumentoLiqAGerar, strSerieLiq, lngNumDocLiqAnterior, clsDocumentoVenda.DataDoc);
								}

							}

						}

						//V7 - BD
						if (!clsDocumentoVenda.EmModoEdicao)
						{

							m_objErpBSO.Base.Series.ActualizaNumerador(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, clsDocumentoVenda.DataDoc);

						}

					}

					//LIQUIDAÇÃO DAS RETENÇÕES
					if (((Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencao) & ((clsTabVenda.LiquidaRetencaoIntroducao) ? -1 : 0)) | (Convert.ToInt32(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencaoGarantia) & ((clsTabVenda.LiquidaRetencaoGarantiaIntroducao) ? -1 : 0))) != 0)
					{

						NumPrest = clsDocumentoVenda.Prestacoes.NumItens;

						//Se o documento não tem prestações
						if (NumPrest == 0)
						{
							NumPrest = 1;
						}

						for (lngPos = 1; lngPos <= NumPrest; lngPos++)
						{

							if (clsDocumentoVenda.Prestacoes.NumItens > 0)
							{

								switch(Convert.ToInt32(Double.Parse(clsDocumentoVenda.Prestacoes.GetEdita(lngPos).TipoLinha)))
								{
									case 30 : case 40 : case 41 : case 50 : case 51 : case 70 : case 80 : case 90 : case 60 : case 65 : 
										//Neste caso não faz nada 
										 
										break;
									default:
										 
										if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencao))
										{

											if (clsTabVenda.LiquidaRetencaoIntroducao)
											{

												m_objErpBSO.PagamentosRecebimentos.Liquidacoes.LiquidaRetencao(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Filial, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, clsDocumentoVenda.Serie, m_objErpBSO.DSO.Plat.Utils.FInt(lngPos), 0, clsTabVenda.DocumentoLiquidacaoRetencao, IdDocLiqRet);

											}

										} 
										 
										if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencaoGarantia))
										{

											if (clsTabVenda.LiquidaRetencaoGarantiaIntroducao)
											{

												m_objErpBSO.PagamentosRecebimentos.Liquidacoes.LiquidaRetencaoGarantia(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Filial, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, clsDocumentoVenda.Serie, m_objErpBSO.DSO.Plat.Utils.FInt(lngPos), 0, clsTabVenda.DocumentoLiquidacaoRetencaoGarantia, IdDocLiqRetGar);

											}

										} 
										 
										break;
								}

							}
							else
							{

								if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencao))
								{

									if (clsTabVenda.LiquidaRetencaoIntroducao)
									{

										m_objErpBSO.PagamentosRecebimentos.Liquidacoes.LiquidaRetencao(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Filial, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, clsDocumentoVenda.Serie, m_objErpBSO.DSO.Plat.Utils.FInt(lngPos), 0, clsTabVenda.DocumentoLiquidacaoRetencao, IdDocLiqRet);

									}

								}

								if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Params.SujeitoRetencaoGarantia))
								{

									if (clsTabVenda.LiquidaRetencaoGarantiaIntroducao)
									{

										m_objErpBSO.PagamentosRecebimentos.Liquidacoes.LiquidaRetencaoGarantia(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Filial, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, clsDocumentoVenda.Serie, m_objErpBSO.DSO.Plat.Utils.FInt(lngPos), 0, clsTabVenda.DocumentoLiquidacaoRetencaoGarantia, IdDocLiqRetGar);

									}

								}

							}

						}

					}
					//FIM LIQUIDAÇÃO DAS RETENÇÕES

					//Transicção do estado do projecto
					if (Convert.ToBoolean(m_objErpBSO.Projectos.Licenca.Projectos.Base))
					{

						if (Strings.Len(clsDocumentoVenda.IDObra) > 0)
						{

							m_objErpBSO.Projectos.Projectos.TransitaEstadoId(clsDocumentoVenda.IDObra, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc);

						}

					}

					//Estorna a facturação e o processamento de avenças a partir dos gabinetes
					EstornaDocumentosGabinetes(clsDocumentoVenda.IDEstorno, ref strAvisos);

					//CR.406-Closing Sales Opportunities improvements
					ActualizaOPV(clsDocumentoVenda);

					//Hades CS.1577 - Rascunho nos Documentos de Venda
					if (clsDocumentoVenda.AntRascunho)
					{

						//BID 592410 : alterar a chave dos anexos associados ao documento gravado em rascunho (o código "41" corresponde às Vendas)
						strSQL = "UPDATE Anexos" + 
						         " SET Chave='" + clsDocumentoVenda.Tipodoc + m_objErpBSO.DSO.Plat.Utils.FStr(clsDocumentoVenda.NumDoc) + clsDocumentoVenda.Serie + clsDocumentoVenda.Filial + "'" + 
						         " FROM Anexos" + 
						         " WHERE Tabela = 41" + 
						         " AND Chave = (SELECT TipoDoc+CAST(NumDoc AS NVARCHAR)+Serie+Filial+'_RASCUNHO' FROM CabecDocRascunhos WHERE Id = '" + clsDocumentoVenda.ID + "')";
						DbCommand TempCommand_5 = null;
						TempCommand_5 = m_objErpBSO.DSO.BDAPL.CreateCommand();
						UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_5);
						TempCommand_5.CommandText = strSQL;
						UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_5);
						TempCommand_5.ExecuteNonQuery();

						string tempRefParam27 = clsDocumentoVenda.ID;
						RemoveRascunhoID(tempRefParam27);

					}

					//CS.1809
					//Associação automática a uma Cessão de Factoring
					if (FuncoesComuns100.FuncoesBS.Utils.FactoringDisponivel() && (!clsDocumentoVenda.EmModoEdicao) || blnRemoveuCessao)
					{

						m_objErpBSO.Factoring.Integracoes.IntegraDocumentoCessao(clsDocumentoVenda);

					}

					//Removemos as linhas de adiantamento para  recalcular os totais e obter o resumo de iva para permitir atualizar os campos novos na ResumoIVA
					if (clsDocumentoVenda.TrataIvaCaixa)
					{

						arrLinhasAdiantamento = new VndBE100.VndBELinhaDocumentoVenda[1];

						int tempForVar3 = clsDocumentoVenda.Linhas.NumItens;
						for (int lngLinha = 1; lngLinha <= tempForVar3; lngLinha++)
						{

							if (clsDocumentoVenda.Linhas.GetEdita(lngLinha).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAdiantamentos && Strings.Len(clsDocumentoVenda.Linhas.GetEdita(lngLinha).DadosAdiantamento.IDHistorico) != 0)
							{

								arrLinhasAdiantamento[arrLinhasAdiantamento.GetUpperBound(0)] = clsDocumentoVenda.Linhas.GetEdita(lngLinha);
								clsDocumentoVenda.Linhas.Remove(lngLinha);
								lngLinha--;
								arrLinhasAdiantamento = ArraysHelper.RedimPreserve(arrLinhasAdiantamento, new int[]{arrLinhasAdiantamento.GetUpperBound(0) + 2});

							}

							if (lngLinha >= clsDocumentoVenda.Linhas.NumItens)
							{
								break;
							}

						}

						if (arrLinhasAdiantamento.GetUpperBound(0) > 0)
						{

							CalculaTotaisDocumento(ref clsDocumentoVenda);

							int tempForVar4 = clsDocumentoVenda.ResumoIva.NumItens;
							for (int lngLinha = 1; lngLinha <= tempForVar4; lngLinha++)
							{

								strSQL = "UPDATE ResumoIVA SET IncidenciaADI = @1@, ValorADI = @2@  WHERE Id= '@3@' AND CodIva = '@4@'";
								dynamic[] tempRefParam28 = new dynamic[]{clsDocumentoVenda.ResumoIva.GetEdita(lngLinha).Incidencia, clsDocumentoVenda.ResumoIva.GetEdita(lngLinha).Valor, clsDocumentoVenda.ID, clsDocumentoVenda.ResumoIva.GetEdita(lngLinha).CodIva};
								strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam28);
								DbCommand TempCommand_6 = null;
								TempCommand_6 = m_objErpBSO.DSO.BDAPL.CreateCommand();
								UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_6);
								TempCommand_6.CommandText = strSQL;
								UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_6);
								TempCommand_6.ExecuteNonQuery();

							}

							for (int lngLinha = 0; lngLinha <= arrLinhasAdiantamento.GetUpperBound(0) - 1; lngLinha++)
							{

								clsDocumentoVenda.Linhas.Insere(arrLinhasAdiantamento[lngLinha]);

							}

							blnEmModoEdicao = clsDocumentoVenda.EmModoEdicao;

							if (!blnEmModoEdicao)
							{

								clsDocumentoVenda.EmModoEdicao = true;

							}

							CalculaTotaisDocumento(ref clsDocumentoVenda);

							clsDocumentoVenda.EmModoEdicao = blnEmModoEdicao;

						}

						//UPGRADE_NOTE: (1061) Erase was upgraded to System.Array.Clear. More Information: http://www.vbtonet.com/ewis/ewi1061.aspx
						if (arrLinhasAdiantamento != null)
						{
							Array.Clear(arrLinhasAdiantamento, 0, arrLinhasAdiantamento.Length);
						}

					}

				}
				else
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.Actualiza", StrErro);

				}



				//    'LIGAÇÃO A STP
				if (clsTabVenda.LigacaoSTP)
				{
					m_objErpBSO.ServicosTecnicos.Processos.IntegraDocumentoVenda(clsDocumentoVenda, clsTabVenda, strAvisos);
				}
				//    ^LIGAÇÃO A STP

				if (clsDocumentoVenda.Callbacks != null)
				{
					foreach (StdBE100.IStdBECallback callback2 in clsDocumentoVenda.Callbacks)
					{
						callback = callback2;
						callback.Callback(StdBE100.StdBETipos.EnumTipoEventoCallback.ecAntesDeTerminarTransacao, clsDocumentoVenda);
						callback = null;
					}

				}

				m_objErpBSO.TerminaTransaccao();



				blnIniciouTrans = false;

				//TUNNING
				objSerie = null;
				clsTabVenda = null;
				ClsLinhaVenda = null;
				objCamposTTE = null;
				callback = null;

				//CS.599 - WorkFlow Events
				FuncoesComuns100.FuncoesBS.Utils.CallWorkFlowEvents(clsDocumentoVenda, ConstantesPrimavera100.Modulos.Vendas);

				// Tratamento do LOG
				bool tempRefParam29 = true;
				m_objErpBSO.DSO.Vendas.Documentos.set_LogActivo(ref tempRefParam29);

				objPrestacoes = null;

				//CS.3405 - Adiantamentos em CC
				objLiquidacao = null;
			}
			catch (Exception e)
			{

				if (blnIniciouTrans)
				{
					m_objErpBSO.DesfazTransaccao();
				}

				// Tratamento do LOG
				m_objErpBSO.DSO.LogActivo = true;


				//CS.3405 - Adiantamentos em CC

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				if (Information.Err().Number == StdErros.StdErroPrevisto)
				{

					if (StrErro == e.Message)
					{

						//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
						StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Actualiza", e.Message);

					}
					else
					{

						//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
						StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Actualiza", StrErro + Environment.NewLine + e.Message);

					}

				}
				else
				{

					//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
					StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Actualiza", e.Message);

				}
			}

		}

		public void Actualiza( VndBEDocumentoVenda clsDocumentoVenda,  string strAvisos,  string IdDocLiqRet)
		{
			string tempRefParam119 = "";
			Actualiza( clsDocumentoVenda,  strAvisos,  IdDocLiqRet,  tempRefParam119);
		}

		public void Actualiza( VndBEDocumentoVenda clsDocumentoVenda,  string strAvisos)
		{
			string tempRefParam120 = "";
			string tempRefParam121 = "";
			Actualiza( clsDocumentoVenda,  strAvisos,  tempRefParam120,  tempRefParam121);
		}

		public void Actualiza( VndBEDocumentoVenda clsDocumentoVenda)
		{
			string tempRefParam122 = "";
			string tempRefParam123 = "";
			string tempRefParam124 = "";
			Actualiza( clsDocumentoVenda,  tempRefParam122,  tempRefParam123,  tempRefParam124);
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : CalculaTotaisDocumento
		// Description : Faz o cálculo dos totais do documento pela U2LCalc ou de forma manual (conforme o tratamento do documento)
		// Arguments   : DocVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void CalculaTotaisDocumento(ref VndBE100.VndBEDocumentoVenda DocVenda)
		{

			//Se não faz cálculo manual então pode calcular automaticamente
			if (!DocVenda.CalculoManual)
			{

				CalculaValoresTotais(ref DocVenda);

			}

		}

		private void ActualizaOPV(VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{
			//CR.406-Closing Sales Opportunities improvements
			//Fecha automaticamente as OPV caso as linhas tenham vindo de dum doc Interno com OPV associada

			//--------------------------------------------------------
			BasBELinhasRastreabilidade clsLinhas = null;
			BasBELinhaRastreabilidade clsLinha = null;
			StdBE100.StdBELista objLista = null;
			//--------------------------------------------------------
			try
			{

				if ((Convert.ToInt32(m_objErpBSO.CRM.LicencaCRM.Base) | ((m_objErpBSO.Licenca.VersaoDemo) ? -1 : 0)) != 0)
				{ //Verifica as licencas

					if (Convert.ToBoolean(m_objErpBSO.CRM.Params.FecharOPVAutomaticamente))
					{ //A opção tem que estar activada no administrador

						string tempRefParam = clsDocumentoVenda.Tipodoc;
						string tempRefParam2 = "TipoDocumento";
						if (m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam, tempRefParam2)) == ((short) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro))
						{
							//Caso seja um documento financeiro

							int tempForVar = clsDocumentoVenda.Linhas.NumItens;
							for (int lngLinha = 1; lngLinha <= tempForVar; lngLinha++)
							{
								//Verifica a rastreabilidade do documento..
								clsLinhas = new BasBELinhasRastreabilidade();
								clsLinha = new BasBELinhaRastreabilidade();

								// Guardar a informação do documento inicial

								clsLinha.IDCabecDoc = clsDocumentoVenda.ID;
								clsLinha.IDDestino = clsDocumentoVenda.Linhas.GetEdita(lngLinha).IdLinha;
								clsLinha.Nivel = (int) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro;
								clsLinha.Filial = clsDocumentoVenda.Filial;
								clsLinha.Serie = clsDocumentoVenda.Serie;
								clsLinha.Tipodoc = clsDocumentoVenda.Tipodoc;
								clsLinha.NumDoc = clsDocumentoVenda.NumDoc;
								clsLinha.Modulo = ConstantesPrimavera100.Modulos.Vendas;
								clsLinha.Data = clsDocumentoVenda.DataDoc;
								clsLinha.Quantidade = clsDocumentoVenda.Linhas.GetEdita(lngLinha).Quantidade;
								clsLinha.QuantTransformada = m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Internos.Documentos.DaQuantidadeCopiada(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Linhas.GetEdita(lngLinha).IdLinha, false));

								clsLinha.Estado = clsDocumentoVenda.Linhas.GetEdita(lngLinha).Fechado.ToString();


								clsLinhas.Insere(clsLinha);
								clsLinha = null;

								//Rastreabilidade das linhas copiadas
								string tempRefParam3 = clsDocumentoVenda.Linhas.GetEdita(lngLinha).IdLinha;
								m_objErpBSO.Vendas.Documentos.ProcuraLinhasAnteriores(tempRefParam3, clsLinhas);
								//m_objErpBSO.Internos.Documentos.ProcuraLinhasAnteriores clsDocumentoVenda.Linhas.Edita(lngLinha).IdLinha, clsLinhas

								int tempForVar2 = clsLinhas.NumItens;
								for (int lngLinhaAUX = 1; lngLinhaAUX <= tempForVar2; lngLinhaAUX++)
								{
									//Procura nas linhas de Rastreabilidade, se existe uma linha de cotação

									string tempRefParam4 = clsLinhas.GetEdita(lngLinhaAUX).Tipodoc;
									string tempRefParam5 = "TipoDocumento";
									if (m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam4, tempRefParam5)) == ((short) BasBETipos.LOGTipoDocumento.LOGDocCotacao))
									{
										//So fecha Oportunidades de venda se o Documento Interno é do Tipo OPV

										//Verificamos se esta linha de cotação veio de uma Oportunidade de Venda
										string tempRefParam6 = "SELECT TOP 1 IdOportunidade FROM CabecDoc WITH (NOLOCK) WHERE ID = '" + clsLinhas.GetEdita(lngLinhaAUX).IDCabecDoc + "' ";
										objLista = m_objErpBSO.Consulta(tempRefParam6); //PriGlobal: IGNORE

										if (objLista != null)
										{

											if (!objLista.NoFim())
											{

												//Se veio de uma OPV, então actualizamos o estado dessa OPV
												if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("IdOportunidade"))) > 0)
												{

													//Actualiza o estado da oportunidade para Ganho
													m_objErpBSO.CRM.OportunidadesVenda.ActualizaValorAtributoID(m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("IdOportunidade")), "EstadoVenda", 1); // Ganha 'PriGlobal: IGNORE

													break;

												}

											}

										}

									}

								}

								objLista = null;
								clsLinhas = null;

							}

						}

					}

				}

				//kill memory objects
				objLista = null;
				clsLinhas = null;
				clsLinha = null;
			}
			catch (System.Exception excep)
			{

				//kill memory objects

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaOPV", excep.Message);
			}

		}

		private void CalculaEstadoLinha(VndBE100.VndBELinhaDocumentoVenda LinhaVenda)
		{

			try
			{


				if (Strings.Len(LinhaVenda.Estado) == 0)
				{

					if (LinhaVenda.QuantSatisfeita == 0)
					{

						LinhaVenda.Estado = ConstantesPrimavera100.Documentos.EstadoLinPendente;

					}
					else if (Math.Abs(LinhaVenda.QuantSatisfeita) >= Math.Abs(LinhaVenda.Quantidade))
					{ 

						LinhaVenda.Estado = ConstantesPrimavera100.Documentos.EstadoLinTransformado; // "T"

					}
					else
					{

						LinhaVenda.Estado = ConstantesPrimavera100.Documentos.EstadoLinPendente; // "P"

					}

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.CalculaEstadoLinha", excep.Message);
			}


		}

		private double DaValoresLinhaTrans(ref string IdLinha, ref StdBE100.StdBELista objCampos)
		{
			double TotalActualizar = 0;
			double TotalConvert = 0;
			string strSQL = "";

			try
			{

				//CS.242_7.50_Alfa8 - Adicionado o "ValorIEC"
				//CS.3693 (foi adicionado o campo "CabecDoc.Versao")
				strSQL = "SELECT DV.TipoDocumento, CD.Entidade, CD.TipoEntidade, CD.Moeda, CD.MoedaDaUEM, CD.Cambio, CD.CambioMBase, CD.CambioMAlt, CD.Arredondamento, CD.ArredondamentoIva, CD.RegimeIva, CD.DescPag, CD.DescEntidade, " + 
				         "LD.IDCabecDoc, LD.Artigo, LD.Armazem, LD.Lote, LD.Localizacao, LD.Desconto1, LD.Desconto2, LD.Ecotaxa, LD.CodIvaEcotaxa, LD.TaxaIvaEcotaxa, LD.Desconto3, LD.TaxaIva, LD.CodIva, LD.PrecUnit, LD.TipoLinha, LD.FactorConv, LD.Arred, LDS.Quantidade, LDS.QuantReserv, LDS.QuantTrans, LD.TaxaRecargo, LD.PercIncidenciaIVA , LD.IvaRegraCalculo, LD.ValorIEC, LD.BaseCalculoIncidencia, LD.RegraCalculoIncidencia, CD.Versao " + 
				         ", DV.TipoDocSTK " + 
				         "FROM CabecDoc CD " + 
				         "INNER JOIN DocumentosVenda DV ON CD.TipoDoc = DV.Documento " + 
				         "INNER JOIN LinhasDoc LD ON CD.ID = LD.IDCabecDoc " + 
				         "INNER JOIN LinhasDocStatus LDS ON LD.ID = LDS.IDLinhasDoc " + 
				         "WHERE LD.ID='@1@'";
				dynamic[] tempRefParam = new dynamic[]{IdLinha};
				strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

				objCampos = m_objErpBSO.Consulta(strSQL);

				if (!objCampos.NoFim())
				{
					//CS.242_7.50_Alfa8 - Adicionado o "ValorIEC"
					//CS.3693 (foi adicionado o parâmetro "Versao")
					//BID 591151 : foi adicionada a multiplicação do Factor de Conversão ao valor da Ecotaxa
					if (objCampos.Valor("Versao") == "08.02" || String.CompareOrdinal(objCampos.Valor("Versao"), "09.01") >= 0)
					{
						//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
						TotalActualizar = FuncoesComuns100.FuncoesBS.Documentos.CalculaTotal(Double.Parse(objCampos.Valor("Quantidade")) - Double.Parse(objCampos.Valor("QuantTrans")), Double.Parse(objCampos.Valor("DescEntidade")), Double.Parse(objCampos.Valor("DescPag")), Convert.ToInt32(Double.Parse(objCampos.Valor("RegimeIva"))), Convert.ToInt32(Double.Parse(objCampos.Valor("Arredondamento"))), Convert.ToInt32(Double.Parse(objCampos.Valor("ArredondamentoIva"))), Double.Parse(objCampos.Valor("Desconto1")), Double.Parse(objCampos.Valor("Desconto2")), Double.Parse(objCampos.Valor("Desconto3")), Convert.ToInt32(Double.Parse(objCampos.Valor("TipoLinha"))), objCampos.Valor("CodIva"), Double.Parse(objCampos.Valor("PrecUnit")), Double.Parse(objCampos.Valor("TaxaIva")), Double.Parse(objCampos.Valor("TaxaRecargo")), Double.Parse(objCampos.Valor("PercIncidenciaIVA")), Double.Parse(objCampos.Valor("Ecotaxa")) * Double.Parse(objCampos.Valor("FactorConv")), objCampos.Valor("CodIvaEcotaxa"), Double.Parse(objCampos.Valor("TaxaIvaEcotaxa")), Convert.ToInt32(Double.Parse(objCampos.Valor("IvaRegraCalculo"))), Double.Parse(objCampos.Valor("ValorIEC")), m_objErpBSO.DSO.Plat.Utils.FDbl(objCampos.Valor("BaseCalculoIncidencia")), (BasBETiposGcp.EnumRegimesIncidenciaIva) m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.Valor("RegraCalculoIncidencia")), objCampos.Valor("Versao"), m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.Valor("TipoDocSTK")), ConstantesPrimavera100.Modulos.Vendas);
					}
					else
					{
						//Fim 591151
						//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
						TotalActualizar = FuncoesComuns100.FuncoesBS.Documentos.CalculaTotal(Double.Parse(objCampos.Valor("Quantidade")) - Double.Parse(objCampos.Valor("QuantTrans")), Double.Parse(objCampos.Valor("DescEntidade")), Double.Parse(objCampos.Valor("DescPag")), Convert.ToInt32(Double.Parse(objCampos.Valor("RegimeIva"))), Convert.ToInt32(Double.Parse(objCampos.Valor("Arredondamento"))), Convert.ToInt32(Double.Parse(objCampos.Valor("ArredondamentoIva"))), Double.Parse(objCampos.Valor("Desconto1")), Double.Parse(objCampos.Valor("Desconto2")), Double.Parse(objCampos.Valor("Desconto3")), Convert.ToInt32(Double.Parse(objCampos.Valor("TipoLinha"))), objCampos.Valor("CodIva"), Double.Parse(objCampos.Valor("PrecUnit")), Double.Parse(objCampos.Valor("TaxaIva")), Double.Parse(objCampos.Valor("TaxaRecargo")), Double.Parse(objCampos.Valor("PercIncidenciaIVA")), Double.Parse(objCampos.Valor("Ecotaxa")), objCampos.Valor("CodIvaEcotaxa"), Double.Parse(objCampos.Valor("TaxaIvaEcotaxa")), Convert.ToInt32(Double.Parse(objCampos.Valor("IvaRegraCalculo"))), Double.Parse(objCampos.Valor("ValorIEC")), m_objErpBSO.DSO.Plat.Utils.FDbl(objCampos.Valor("BaseCalculoIncidencia")), (BasBETiposGcp.EnumRegimesIncidenciaIva) m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.Valor("RegraCalculoIncidencia")), objCampos.Valor("Versao"), m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.Valor("TipoDocSTK")), ConstantesPrimavera100.Modulos.Vendas);
					}
					bool tempBool2 = false;
					string auxVar_2 = objCampos.Valor("MoedaDaUEM");
					bool tempBool = false;
					string auxVar = objCampos.Valor("MoedaDaUem");
					TotalConvert = StdPriAPIs.ConverteMOrigMEsp(TotalActualizar, Double.Parse(objCampos.Valor("Cambio")), (((Boolean.TryParse(auxVar, out tempBool)) ? tempBool : Convert.ToBoolean(Double.Parse(auxVar)))) ? 1 : 0, Double.Parse(objCampos.Valor("Cambio")), (((Boolean.TryParse(auxVar_2, out tempBool2)) ? tempBool2 : Convert.ToBoolean(Double.Parse(auxVar_2)))) ? 1 : 0, Convert.ToInt32(Double.Parse(objCampos.Valor("Arredondamento"))));

				}


				return TotalConvert;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValoresLinhaTrans", excep.Message);
			}
			return 0;
		}

		private void EfectuaLiquidacaoAutomatica(bool blnExisteDocLiquidacao, string FilialLiq, int NumDocLiq, string strSerieLiq, string DocLiq, string EstadoLiq, VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{
			dynamic ClsDocLiq = null;
			StdBE100.StdBECampos objCampos = null;
			OrderedDictionary objValoresPendentes = null;
			string strIdCabecMovCBL = "";
			dynamic objDocLiqOrg = null;
			bool blnExistemPend = false; //BID:583572

			try
			{

				//BID:583572
				blnExistemPend = false;

				//ClsDocLiq = new dynamic();
				objValoresPendentes = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);


				ClsDocLiq.TipoEntidade = clsDocumentoVenda.TipoEntidadeFac;
				ClsDocLiq.Entidade = clsDocumentoVenda.EntidadeFac;
				ClsDocLiq.Tipodoc = DocLiq;
				ClsDocLiq.Serie = strSerieLiq;
				ClsDocLiq.Moeda = clsDocumentoVenda.Moeda;
				ClsDocLiq.Posto = clsDocumentoVenda.Posto;
				ClsDocLiq.Utilizador = clsDocumentoVenda.Utilizador;

				ClsDocLiq.IDObra = clsDocumentoVenda.IDObra;
				ClsDocLiq.WBSItem = clsDocumentoVenda.WBSItem;


				//Preenche o objecto com os dados sugeridos para o documento
				m_objErpBSO.PagamentosRecebimentos.Liquidacoes.PreencheDadosRelacionados(ClsDocLiq);

				//BID 568532/570017
				string tempRefParam = "CabLiq";
				ClsDocLiq.CamposUtil = FuncoesComuns100.FuncoesBS.Utils.PreencheCamposUtil(ref tempRefParam); //PriGlobal: IGNORE

				if (blnExisteDocLiquidacao)
				{

					ClsDocLiq.Serie = strSerieLiq;
					ClsDocLiq.Filial = FilialLiq;
					ClsDocLiq.NumDoc = NumDocLiq.ToString();
					ClsDocLiq.EmModoEdicao = true;

				}

				ClsDocLiq.ModoPag = clsDocumentoVenda.ModoPag; //BID 574785
				ClsDocLiq.Moeda = clsDocumentoVenda.Moeda;
				ClsDocLiq.Cambio = clsDocumentoVenda.Cambio;
				ClsDocLiq.CambioMBase = clsDocumentoVenda.CambioMBase;
				ClsDocLiq.CambioMAlt = clsDocumentoVenda.CambioMAlt;
				ClsDocLiq.CorreccaoMonetaria = 0;

				ClsDocLiq.MoedaDaUEM = clsDocumentoVenda.MoedaDaUEM;
				ClsDocLiq.DataDoc = clsDocumentoVenda.DataDoc;
				ClsDocLiq.DataIntroducao = clsDocumentoVenda.DataDoc;
				//----------------------------------------------------


				if (clsDocumentoVenda.GeraPendentePorLinha)
				{

					foreach (BasBEPrestacao ObjPrestacao in clsDocumentoVenda.Prestacoes)
					{
						switch(Convert.ToInt32(Double.Parse(ObjPrestacao.TipoLinha)))
						{
							case 30 : case 40 : case 41 : case 50 : case 51 : case 70 : case 80 : case 90 : case 60 : case 65 : 
								//Neste caso não faz nada 
								 
								break;
							default:
								 
								//BID:583572 
								if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Pendentes.Existe(clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, ObjPrestacao.NumPrestacao, 0)))
								{

									blnExistemPend = true;

									//Adiciona o documento de venda ao documento de liquidação
									m_objErpBSO.PagamentosRecebimentos.Liquidacoes.AdicionaLinha(ClsDocLiq, clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, ObjPrestacao.NumPrestacao, EstadoLiq, 0, null, null, clsDocumentoVenda.TotalRetencao); //O valor pendente do documento é zero, porque foi liquidado pela totalidade

									ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).ValorPend = 0;
									ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).EmModoEdicao = clsDocumentoVenda.EmModoEdicao;
									ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).IDObra = clsDocumentoVenda.IDObra;

									//BID 568532/570017
									string tempRefParam2 = "LinhasLiq";
									ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).CamposUtil = FuncoesComuns100.FuncoesBS.Utils.PreencheCamposUtil(ref tempRefParam2); //PriGlobal: IGNORE

									//BID:547426 - Como o pendente já existe na BD, guarda os seus valores para depois os repor quando remover a liquidação
									if (blnExisteDocLiquidacao)
									{

										//BID 569822 (foi adicionado o campo "NumPrestacao")
										objCampos = (StdBE100.StdBECampos) m_objErpBSO.PagamentosRecebimentos.Pendentes.DaValorAtributos(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, ObjPrestacao.NumPrestacao, clsDocumentoVenda.Serie, clsDocumentoVenda.Filial, EstadoLiq, 0, "ValorPendente", "ValorRetencaoPendente", "ValorRetencaoGarantiaPendente", "IdHistorico", "NumPrestacao");

										objValoresPendentes.Add(m_objErpBSO.DSO.Plat.Utils.FStr(ClsDocLiq.LinhasLiquidacao.NumItens), objCampos);
										//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
										string tempRefParam3 = "IdHistorico";
										ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).IDHistorico = ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam3).Valor);

										objCampos = null;

									}
									else
									{

										ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).IDHistorico = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.PagamentosRecebimentos.Pendentes.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, ObjPrestacao.NumPrestacao, clsDocumentoVenda.Serie, clsDocumentoVenda.Filial, EstadoLiq, 0, "IdHistorico"));

									}
									//END BID:547426

								} 
								 
								break;
						}

					}

				}
				else
				{

					//BID:583572
					if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Pendentes.Existe(clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, 1, 0)))
					{

						blnExistemPend = true;

						//Adiciona o documento de venda ao documento de liquidação
						m_objErpBSO.PagamentosRecebimentos.Liquidacoes.AdicionaLinha(ClsDocLiq, clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, 1, EstadoLiq, 0, null, null, clsDocumentoVenda.TotalRetencao); //O valor pendente do documento é zero, porque foi liquidado pela totalidade

						ClsDocLiq.LinhasLiquidacao.GetEdita(1).ValorPend = 0;
						ClsDocLiq.LinhasLiquidacao.GetEdita(1).EmModoEdicao = clsDocumentoVenda.EmModoEdicao;
						ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).IDObra = clsDocumentoVenda.IDObra;

						//BID 568532/570017
						string tempRefParam4 = "LinhasLiq";
						ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).CamposUtil = FuncoesComuns100.FuncoesBS.Utils.PreencheCamposUtil(ref tempRefParam4); //PriGlobal: IGNORE

						//BID:547426 - Como o pendente já existe na BD, guarda os seus valores para depois os repor quando remover a liquidação
						if (blnExisteDocLiquidacao)
						{

							//BID 569822 (foi adicionado o campo "NumPrestacao")
							objCampos = (StdBE100.StdBECampos) m_objErpBSO.PagamentosRecebimentos.Pendentes.DaValorAtributos(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, 1, clsDocumentoVenda.Serie, clsDocumentoVenda.Filial, EstadoLiq, 0, "ValorPendente", "ValorRetencaoPendente", "ValorRetencaoGarantiaPendente", "IdHistorico", "NumPrestacao");

							objValoresPendentes.Add("1", objCampos);
							//UPGRADE_WARNING: (1068) objCampos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							string tempRefParam5 = "IdHistorico";
							ClsDocLiq.LinhasLiquidacao.GetEdita(1).IDHistorico = ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam5).Valor);

							objCampos = null;

						}
						else
						{

							ClsDocLiq.LinhasLiquidacao.GetEdita(1).IDHistorico = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.PagamentosRecebimentos.Pendentes.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, 1, clsDocumentoVenda.Serie, clsDocumentoVenda.Filial, EstadoLiq, 0, "IdHistorico"));

						}
						//END BID:547426

					}

				}

				//Documentos de devolução --> Efectuar liquidação auto
				if (clsDocumentoVenda.Devolucoes != null)
				{


					foreach (VndBE100.VndBEDevolucaoVenda docDev in clsDocumentoVenda.Devolucoes)
					{

						m_objErpBSO.PagamentosRecebimentos.Liquidacoes.AdicionaLinha(ClsDocLiq, docDev.Filial, docDev.Modulo, docDev.Tipodoc, docDev.Serie, docDev.NumDoc, m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributoID(docDev.IDHistorico, "NumPrestacao"), docDev.Estado, 0, docDev.ValorParcial * FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(docDev.Modulo, docDev.Tipodoc));

						//BID 568532/570017
						string tempRefParam6 = "LinhasLiq";
						ClsDocLiq.LinhasLiquidacao.GetEdita(ClsDocLiq.LinhasLiquidacao.NumItens).CamposUtil = FuncoesComuns100.FuncoesBS.Utils.PreencheCamposUtil(ref tempRefParam6); //PriGlobal: IGNORE

						//BID:583572
						blnExistemPend = true;

					}

				}

				//BID:583572
				if (blnExistemPend)
				{

					//BID:547426 - Remove a liquidacao que foi gerada anteriormente
					if (blnExisteDocLiquidacao)
					{

						//BID: 583062
						//Se estamos a regravar um documento editamos a liquidação para obter os CDU's existentes
						objDocLiqOrg = (dynamic) m_objErpBSO.PagamentosRecebimentos.Liquidacoes.Edita(FilialLiq, DocLiq, strSerieLiq, NumDocLiq);

						if (objDocLiqOrg != null)
						{

							ClsDocLiq.CamposUtil = objDocLiqOrg.CamposUtil;

							int tempForVar = ClsDocLiq.LinhasLiquidacao.NumItens;
							for (int lngI = 1; lngI <= tempForVar; lngI++)
							{

								int tempForVar2 = objDocLiqOrg.LinhasLiquidacao.NumItens;
								for (int lngJ = lngI; lngJ <= tempForVar2; lngJ++)
								{

									if (ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).IDHistorico == objDocLiqOrg.LinhasLiquidacao.GetEdita(lngJ).IDHistorico)
									{

										ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).CamposUtil = objDocLiqOrg.LinhasLiquidacao.GetEdita(lngJ).CamposUtil;

									}

								}

							}

						}

						objDocLiqOrg = null;

						//FIM BID: 583062

						//BID 553790
						strIdCabecMovCBL = Convert.ToString(m_objErpBSO.PagamentosRecebimentos.Liquidacoes.DaValorAtributo(DocLiq, NumDocLiq, FilialLiq, strSerieLiq, "IdCabecMovCBL"));

						m_objErpBSO.PagamentosRecebimentos.Liquidacoes.Remove(FilialLiq, DocLiq, strSerieLiq, NumDocLiq);

						//BID 553790 (Remover da Contabilidade o documento de liquidação automática)
						if (Strings.Len(strIdCabecMovCBL) > 0)
						{
							m_objErpBSO.Contabilidade.Documentos.RemoveID(strIdCabecMovCBL);
						}
						//Fim 553790

						ClsDocLiq.EmModoEdicao = false;

						//Percorre todos os pendentes e altualiza-os
						for (int intIndex = 1; intIndex <= objValoresPendentes.Count; intIndex++)
						{
							//BID 569822 (foi colocado "objValoresPendentes(m_objErpBSO.DSO.Plat.Utils.FStr(intindex)).Item("NumPrestacao").Valor" em vez de "intIndex")
							string tempRefParam7 = "NumPrestacao";
							m_objErpBSO.PagamentosRecebimentos.Pendentes.ActualizaValorAtributos(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.NumDoc, ((StdBE100.StdBECampos) objValoresPendentes[m_objErpBSO.DSO.Plat.Utils.FStr(intIndex)]).GetItem(ref tempRefParam7).Valor, clsDocumentoVenda.Serie, clsDocumentoVenda.Filial, EstadoLiq, 0, (StdBE100.StdBECampos) objValoresPendentes[m_objErpBSO.DSO.Plat.Utils.FStr(intIndex)]);
							//BID 599733
						}

						int tempForVar3 = ClsDocLiq.LinhasLiquidacao.NumItens;
						for (int lngI = 1; lngI <= tempForVar3; lngI++)
						{

							if (ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).ValorRetencao != 0 || ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).ValorRetencaoGarantia != 0)
							{
								ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).Retencoes = (dynamic) m_objErpBSO.PagamentosRecebimentos.Liquidacoes.CalculaRetencoesLiquidacaoEX(ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).ValorRec, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).ValorRetencao, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).ValorRetencaoGarantia, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).MoedaDocOrig, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).CambioDocOrig, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).MoedaDaUEMDocOrig, clsDocumentoVenda.Arredondamento, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).IDHistorico, ClsDocLiq.LinhasLiquidacao.GetEdita(lngI).Estado);
							}
						}

					}

					//END BID:547426

					//Assim gera as retenções com base no resumo de retenções do pendente
					ClsDocLiq.RetencoesGerar = null;
					//Actualiza a liquidação
					m_objErpBSO.PagamentosRecebimentos.Liquidacoes.Actualiza(ClsDocLiq);

				}


				ClsDocLiq = null;
				//BID:547426
				objCampos = null;
				objValoresPendentes = null;
				//END BID:547426
			}
			catch (System.Exception excep)
			{
				//BID:547426
				//END BID:547426

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.EfectuaLiquidacaoAutomatica", excep.Message);
			}

		}

		//Preenche o objecto documento de venda antes de fazer a actualização.
		//BID 546594 - Adicionado o clsTabVenda
		private void PreencheDocVenda(ref VndBE100.VndBEDocumentoVenda clsDocumentoVenda, VndBE100.VndBETabVenda clsTabVenda)
		{
			bool blnPrestacoes = false; //BID 546594

			try
			{


				//CS.3693
				//.Versao = ConstantesPrimavera100.DOCUMENTOS.VERSAOACTUAL
				if (Strings.Len(clsDocumentoVenda.Versao) == 0)
				{
					clsDocumentoVenda.Versao = ConstantesPrimavera100.Documentos.VersaoActual;
				}
				//Fim CS.3693

				if (Strings.Len(clsDocumentoVenda.ID) == 0)
				{
					bool tempRefParam = true;
					clsDocumentoVenda.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);
				}

				//FIL
				if (Strings.Len(clsDocumentoVenda.Filial) == 0)
				{
					clsDocumentoVenda.Filial = m_objErpBSO.Base.Filiais.CodigoFilial;
				}

				//Se a moeda não estiver preenchida é a moeda do cliente
				if (Strings.Len(clsDocumentoVenda.Moeda) == 0)
				{


					string switchVar = clsDocumentoVenda.TipoEntidade;
					if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
					{
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Clientes.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						clsDocumentoVenda.Moeda = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Clientes.DaValorAtributo(clsDocumentoVenda.Entidade, "Moeda"));

					}
					else
					{
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.OutrosTerceiros.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						clsDocumentoVenda.Moeda = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.OutrosTerceiros.DaValorAtributo(clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidade, "Moeda"));

					}

				}

				if (clsDocumentoVenda.DataUltimaActualizacao == DateTime.FromOADate(0))
				{
					clsDocumentoVenda.DataUltimaActualizacao = DateTime.Now;
				}

				//BID 597855
				//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
				if (!Convert.IsDBNull(clsDocumentoVenda.DataDoc))
				{
					clsDocumentoVenda.DataDoc = m_objErpBSO.DSO.Plat.Utils.FData(clsDocumentoVenda.DataDoc.Year.ToString() + "-" + clsDocumentoVenda.DataDoc.Month.ToString() + "-" + DateAndTime.Day(clsDocumentoVenda.DataDoc).ToString());
				}
				//^BID 597855

				if (Strings.Len(clsDocumentoVenda.CondPag) != 0)
				{

					//BID 546594
					//   As prestações só fazem sentido se o documento ligar às contas correntes

					blnPrestacoes = clsTabVenda.LigacaoCC && (ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.CondsPagamento.DaValorAtributo(clsDocumentoVenda.CondPag, "TipoCondicao")) == "4" || clsDocumentoVenda.GeraPendentePorLinha);

					if (blnPrestacoes)
					{

						//Preenche o objecto com o valor das prestações por defeito e coloca o orçamento no estado de apreciação
						if (clsDocumentoVenda.Prestacoes.NumItens == 0)
						{
							PreencheDadosPrestacao(ref clsDocumentoVenda);
						}

					}
					else
					{

						clsDocumentoVenda.Prestacoes = null;
						clsDocumentoVenda.Prestacoes = new BasBEPrestacoes();

					}

				}

				string tempRefParam2 = clsDocumentoVenda.TipoOperacao;
				clsDocumentoVenda.TipoFiscal = FuncoesComuns100.FuncoesBS.Documentos.DaTipoFiscal(ConstantesPrimavera100.Modulos.Vendas, ref tempRefParam2);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDocVenda", excep.Message);
			}


		}

		//CS.3693 (foi adicionado o parâmetro "Versao")
		private void CalculaValoresIvaNaoDedutivel(double DescCli, double DescPag, int RegimeIva, int Arredondamento, int ArredondaIva, string strVersao, VndBE100.VndBELinhaDocumentoVenda Linha, string MovStock, string Seccao = "1")
		{

			int Apt = 0;
			double Desconto = 0;
			string strCodIva = "";
			double dblTaxaIva = 0;

			try
			{

				if (RegimeIva == 3)
				{ //Intracomunitario
					Apt = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_AlocaTotais();


					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_InicializaTotais(Apt, DescCli, DescPag, 0, 1, (short) Arredondamento, (short) ArredondaIva, 0, 0);

					Desconto = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaDescontoTotal(Linha.Desconto1, Linha.Desconto2, Linha.Desconto3);
					if (Information.IsNumeric(Linha.TipoLinha))
					{
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Artigos.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						strCodIva = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Artigos.DaValorAtributo(Linha.Artigo, "Iva"));
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to double. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						dblTaxaIva = ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Iva.DaValorAtributo(strCodIva, "Taxa"));
						//CS.242_7.50_Alfa8 - Adicionado o "ValorIEC"
						//BID 557608 (foi adicionado o "Arredonda(...,2)" à taxa de Iva)
						//CS.3483 - Regimes especiais de IVA e IPC
						//CS.3693 (foi adicionado o parâmetro "Versao")
						//BID 591151 : foi adicionada a multiplicação do Factor de Conversão ao valor da Ecotaxa
						if (strVersao == "08.02" || String.CompareOrdinal(strVersao, "09.01") >= 0)
						{
							LogPRIAPIs.V10_InsereLinha(Apt, Convert.ToInt32(Double.Parse(Linha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblTaxaIva, 2), Linha.PrecUnit, Desconto, Linha.Quantidade, strCodIva, 0, 0, Linha.TaxaRecargo, Linha.PercIncidenciaIVA, 1, Linha.PercIvaDedutivel, 100, Linha.Ecotaxa * Linha.FactorConv, Linha.CodIvaEcotaxa, Linha.TaxaIvaEcotaxa, Convert.ToInt32(Linha.IvaRegraCalculo), Linha.ValorIEC, Linha.BaseCalculoIncidencia, (int) Linha.RegraCalculoIncidencia, FuncoesComuns100.FuncoesBS.Documentos.IgnoraCalculoMargem(ConstantesPrimavera100.Modulos.Vendas, MovStock, Linha.Quantidade, Linha.RegraCalculoIncidencia), strVersao);
						}
						else
						{
							//Fim 591151
							LogPRIAPIs.V10_InsereLinha(Apt, Convert.ToInt32(Double.Parse(Linha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblTaxaIva, 2), Linha.PrecUnit, Desconto, Linha.Quantidade, strCodIva, 0, 0, Linha.TaxaRecargo, Linha.PercIncidenciaIVA, 1, Linha.PercIvaDedutivel, 100, Linha.Ecotaxa, Linha.CodIvaEcotaxa, Linha.TaxaIvaEcotaxa, Convert.ToInt32(Linha.IvaRegraCalculo), Linha.ValorIEC, Linha.BaseCalculoIncidencia, (int) Linha.RegraCalculoIncidencia, FuncoesComuns100.FuncoesBS.Documentos.IgnoraCalculoMargem(ConstantesPrimavera100.Modulos.Vendas, MovStock, Linha.Quantidade, Linha.RegraCalculoIncidencia), strVersao);
						}
					}

					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaTotais(Apt);

					Linha.IvaNaoDedutivel = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIvaNaoDedutivel(Apt);


					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_LibertaTotais(Apt);
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.CalculaValoresIvaNaoDedutivel", excep.Message);
			}

		}


		public void CalculaValoresTotais(ref VndBEDocumentoVenda clsDocumentoVenda)
		{
			int lngLinha = 0;
			int lngIndex = 0;
			int lngApt = 0;
			double dblDesconto = 0;
			double dblTotalIvaMerc = 0;
			double dblTaxaRetencao = 0;
			double dblValorRetAdt = 0;
			int[] arrLinhas = null;
			double[] arrValoresIva = null;
			TestPrimaveraDarwinSupport.PInvoke.UnsafeNative.Structures.StructCodigosIva arrCodsIva = TestPrimaveraDarwinSupport.PInvoke.UnsafeNative.Structures.StructCodigosIva.CreateInstance();
			bool blnCalculaTotaisVersaoAnterior = false; //BID 523420
			double dblTotalIS = 0;
			//CS.3995 - Iva de Caixa
			OrderedDictionary objColIvaNDedutivel = null;
			double dblIvaNDedutivel = 0;
			string strMovStock = "";
			bool blnIsentoRegimeMargem = false;
			double dblArredPrecUnit = 0; //BID 597915

			try
			{

				arrLinhas = new int[11];
				double[] ArrValores = new double[12];
				arrValoresIva = new double[11];
				double[] arrValoresRecargo = new double[11];
				double[] arrResumo = new double[LogPRIAPIs.NUM_POSICOES_ARRAY_IVA + 1];

				//*********BID 525226 *********************************************************
				//O BID 523420 introduz um erro, pois não é necessário o uso das funções da 6
				blnCalculaTotaisVersaoAnterior = false;
				//*********BID 525226 *********************************************************

				string tempRefParam = clsDocumentoVenda.Tipodoc;
				string tempRefParam2 = "TipoDocSTK";
				strMovStock = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam, tempRefParam2));

				objColIvaNDedutivel = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

				//BID 597915
				dblArredPrecUnit = m_objErpBSO.DSO.Plat.Utils.FDbl(m_objErpBSO.Base.Moedas.DaValorAtributo(clsDocumentoVenda.Moeda, "DecPrecUnit"));

				//BID 523420
				if (blnCalculaTotaisVersaoAnterior)
				{
					lngApt = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_AlocaTotais();
				}
				else
				{
					//Fim 523420
					lngApt = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_AlocaTotais();
				} //BID 523420


				dblTaxaRetencao = 0;

				if (Strings.Len(clsDocumentoVenda.RegimeIva) == 0)
				{
					clsDocumentoVenda.RegimeIva = "0";
				}

				//BID 523420
				if (blnCalculaTotaisVersaoAnterior)
				{
					string tempRefParam3 = m_objErpBSO.Base.Params.CodIvaEcovalor;
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_InicializaTotaisEx2(lngApt, clsDocumentoVenda.DescEntidade, clsDocumentoVenda.DescFinanceiro, (short) Convert.ToInt32(Double.Parse(clsDocumentoVenda.RegimeIva)), (short) m_objErpBSO.Base.Params.TipoDesconto, (short) Convert.ToInt32(clsDocumentoVenda.Arredondamento), (short) Convert.ToInt32(clsDocumentoVenda.ArredondamentoIva), dblTaxaRetencao, ref tempRefParam3, ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Iva.DaValorAtributo(m_objErpBSO.Base.Params.CodIvaEcovalor, "Taxa")));
				}
				else
				{
					//Fim 523420
					//BID 564736 (foi adicionado o "Arredonda(...,2)" aos Descontos)
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_InicializaTotais(lngApt, TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.DescEntidade, 2), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.DescFinanceiro, 2), (short) Convert.ToInt32(Double.Parse(clsDocumentoVenda.RegimeIva)), 1, (short) Convert.ToInt32(clsDocumentoVenda.Arredondamento), (short) Convert.ToInt32(clsDocumentoVenda.ArredondamentoIva), dblTaxaRetencao, (clsDocumentoVenda.SujeitoRecargo) ? ((short) (-1)) : ((short) 0));
				} //BID 523420

				// Para cada linha do documento de venda
				lngLinha = 1;
				foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha in clsDocumentoVenda.Linhas)
				{

					blnIsentoRegimeMargem = FuncoesComuns100.FuncoesBS.Documentos.IgnoraCalculoMargem(ConstantesPrimavera100.Modulos.Vendas, strMovStock, ObjLinha.Quantidade, ObjLinha.RegraCalculoIncidencia);

					// Se o tipo de linha for numerico calcula o valor da linha
					if (Information.IsNumeric(ObjLinha.TipoLinha))
					{

						if (ObjLinha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario && ObjLinha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo)
						{

							//BID 523420
							if (blnCalculaTotaisVersaoAnterior)
							{
								dblDesconto = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_CalculaDescontoTotal(ObjLinha.Desconto1, ObjLinha.Desconto2, ObjLinha.Desconto3);
							}
							else
							{
								//Fim 523420
								dblDesconto = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaDescontoTotal(ObjLinha.Desconto1, ObjLinha.Desconto2, ObjLinha.Desconto3);
							}

							//BID 523420
							if (blnCalculaTotaisVersaoAnterior)
							{
								string tempRefParam4 = ObjLinha.CodIva;
								TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_InsereLinhaEx4(lngApt, Convert.ToInt32(Double.Parse(ObjLinha.TipoLinha)), ObjLinha.TaxaIva, ObjLinha.PrecUnit, dblDesconto, ObjLinha.Quantidade, ref tempRefParam4, 0, (ObjLinha.SujeitoRetencao) ? ((short) (-1)) : ((short) 0), ObjLinha.PercIvaDedutivel, 100, lngLinha, ObjLinha.Ecotaxa);
							}
							else
							{
								//Fim 523420
								//CS.242_7.50_Alfa8 - IEC - Adicionado o "ValorIEC"
								//BID 557608 (foi adicionado o "Arredonda(...,2)" à taxa de Iva)
								//CS.3693 (foi adicionado o parâmetro "Versao")
								//BID 591151 : foi adicionada a multiplicação do Factor de Conversão ao valor da Ecotaxa
								if (clsDocumentoVenda.Versao == "08.02" || String.CompareOrdinal(clsDocumentoVenda.Versao, "09.01") >= 0)
								{
									LogPRIAPIs.V10_InsereLinha(lngApt, Convert.ToInt32(Double.Parse(ObjLinha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ObjLinha.TaxaIva, 2), ObjLinha.PrecUnit, dblDesconto, ObjLinha.Quantidade, ObjLinha.CodIva, 0, (ObjLinha.SujeitoRetencao) ? -1 : 0, ObjLinha.TaxaRecargo, ObjLinha.PercIncidenciaIVA, lngLinha, ObjLinha.PercIvaDedutivel, 100, ObjLinha.Ecotaxa * ObjLinha.FactorConv, ObjLinha.CodIvaEcotaxa, ObjLinha.TaxaIvaEcotaxa, Convert.ToInt32(ObjLinha.IvaRegraCalculo), ObjLinha.ValorIEC, ObjLinha.BaseCalculoIncidencia, (int) ObjLinha.RegraCalculoIncidencia, blnIsentoRegimeMargem, clsDocumentoVenda.Versao);
								}
								else
								{
									//Fim 591151
									LogPRIAPIs.V10_InsereLinha(lngApt, Convert.ToInt32(Double.Parse(ObjLinha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ObjLinha.TaxaIva, 2), ObjLinha.PrecUnit, dblDesconto, ObjLinha.Quantidade, ObjLinha.CodIva, 0, (ObjLinha.SujeitoRetencao) ? -1 : 0, ObjLinha.TaxaRecargo, ObjLinha.PercIncidenciaIVA, lngLinha, ObjLinha.PercIvaDedutivel, 100, ObjLinha.Ecotaxa, ObjLinha.CodIvaEcotaxa, ObjLinha.TaxaIvaEcotaxa, Convert.ToInt32(ObjLinha.IvaRegraCalculo), ObjLinha.ValorIEC, ObjLinha.BaseCalculoIncidencia, (int) ObjLinha.RegraCalculoIncidencia, blnIsentoRegimeMargem, clsDocumentoVenda.Versao);
								}
							} //BID 523420

							//BID 523420
							if (blnCalculaTotaisVersaoAnterior)
							{
								TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_ValoresUltimaLinha(lngApt, ref ArrValores[0]);
							}
							else
							{
								//Fim 523420
								TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ValoresUltimaLinha(lngApt, ref ArrValores[0]);
							} //BID 523420

							//BID 597915
							//CS.3483 - Regimes especiais de IVA e IPC
							//ObjLinha.BaseIncidencia = ArrValores(11)
							if (String.CompareOrdinal(clsDocumentoVenda.Versao, "09.03") >= 0)
							{

								ObjLinha.BaseIncidencia = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ArrValores[11], Convert.ToInt16(dblArredPrecUnit));

							}
							else
							{

								ObjLinha.BaseIncidencia = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ArrValores[11], clsDocumentoVenda.Arredondamento);

							}

							ArrValores[4] = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ArrValores[4], clsDocumentoVenda.Arredondamento); //BID 585250

							//BID 528709 (foi adicionado o teste "Or (objLinha.TipoLinha = 91)")
							if ((StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) >= 10 && StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) <= 29) || (StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) == 91))
							{ //Mercadorias e Serviços
								ObjLinha.TotalIliquido = ArrValores[0];
								ObjLinha.TotalDA = ArrValores[1];
								ObjLinha.PrecoLiquido = ArrValores[4];
							}
							else if (StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) >= 40 && StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) <= 49)
							{  //Descontos em valor Mercadoria / Serviços
								ObjLinha.TotalIliquido = 0;
								ObjLinha.TotalDA = ArrValores[4];
								ObjLinha.PrecoLiquido = 0;
							}
							else
							{
								ObjLinha.TotalIliquido = ArrValores[4];
								ObjLinha.TotalDA = ArrValores[1];
								ObjLinha.PrecoLiquido = ArrValores[4];
							}

							ObjLinha.TotalDC = ArrValores[2];
							ObjLinha.TotalDF = ArrValores[3];
							ObjLinha.DescontoComercial = ObjLinha.TotalDA + ObjLinha.TotalDC;
							ObjLinha.TotalIva = ArrValores[6] + ArrValores[7];
							ObjLinha.TotalRecargo = ArrValores[7];
							ObjLinha.IvaNaoDedutivel = ArrValores[8];
							ObjLinha.TotalEcotaxa = ArrValores[9];
							//CS.242_7.50_Alfa8 - IEC
							ObjLinha.TotalIEC = ArrValores[10];

							//CS.3995 - Iva de Caixa
							if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(ObjLinha.CodIva, objColIvaNDedutivel))
							{

								objColIvaNDedutivel.Add(ObjLinha.CodIva, ObjLinha.IvaNaoDedutivel);

							}
							else
							{
								//Calcula o total de IVA não dedutivel
								dblIvaNDedutivel = (double) objColIvaNDedutivel[ObjLinha.CodIva];
								dblIvaNDedutivel += ObjLinha.IvaNaoDedutivel;

								//Adiciona o total de IVA não dedutivel à coleção
								objColIvaNDedutivel.Remove(ObjLinha.CodIva);
								objColIvaNDedutivel.Add(ObjLinha.CodIva, dblIvaNDedutivel);

							}

							//BID 567408
							if (clsDocumentoVenda.RegimeIva == ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva).ToString())
							{
								ObjLinha.TotalIva = 0;
								ObjLinha.TotalRecargo = 0;
								ObjLinha.IvaNaoDedutivel = 0;
							}
							//Fim 567408

							if (StringsHelper.ToDoubleSafe(ObjLinha.TipoLinha) == 90)
							{
								dblValorRetAdt = Convert.ToDouble(m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributoID(ObjLinha.IDHistorico, "ValorRetencao"));
								//BID 523420
								if (blnCalculaTotaisVersaoAnterior)
								{
									TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_DescontaRetencaoAdiantamento(lngApt, dblValorRetAdt);
								}
								else
								{
									//Fim 523420
									TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_DescontaRetencaoAdiantamento(lngApt, dblValorRetAdt);
								} //BID 523420
							}
						}
						else
						{
							ObjLinha.TotalIliquido = 0;
							ObjLinha.TotalDA = 0;
							ObjLinha.DescontoComercial = 0;
							ObjLinha.PrecoLiquido = 0;
							ObjLinha.TotalDC = 0;
							ObjLinha.TotalDF = 0;
							ObjLinha.TotalIva = 0;
							ObjLinha.TotalRecargo = 0;
							ObjLinha.IvaNaoDedutivel = 0;
							ObjLinha.TotalEcotaxa = 0;
							//CS.242_7.50_Alfa8 - IEC
							ObjLinha.TotalIEC = 0;
						}

					}

					//CS.3693 (foi adicionado o parâmetro "Versao")
					CalculaValoresIvaNaoDedutivel(clsDocumentoVenda.DescEntidade, clsDocumentoVenda.DescFinanceiro, Convert.ToInt32(Double.Parse(clsDocumentoVenda.RegimeIva)), clsDocumentoVenda.Arredondamento, clsDocumentoVenda.ArredondamentoIva, clsDocumentoVenda.Versao, ObjLinha, strMovStock, clsDocumentoVenda.Seccao);

					lngLinha++;
				}

				//BID 523420
				if (blnCalculaTotaisVersaoAnterior)
				{
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_CalculaTotais(lngApt);
				}
				else
				{
					//Fim 523420
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaTotais(lngApt);
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ValoresDiferencasIVA(lngApt, ref arrLinhas[0], ref arrValoresIva[0], ref arrValoresRecargo[0]);
				} //BID 523420


				lngIndex = 0;
				while (arrLinhas[lngIndex] > 0)
				{
					if (arrValoresIva[lngIndex] != 0)
					{
						if (clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAcertos || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAdiantamentos || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinDescontoMercadorias || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinDescServicos)
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalIva -= arrValoresIva[lngIndex];
						}
						else
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalIva += arrValoresIva[lngIndex];
						}
					}
					if (arrValoresRecargo[lngIndex] != 0)
					{
						if (clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAcertos || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAdiantamentos || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinDescontoMercadorias || clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinDescServicos)
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalRecargo -= arrValoresRecargo[lngIndex];
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalIva -= arrValoresRecargo[lngIndex]; //BID 533409
						}
						else
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalRecargo += arrValoresRecargo[lngIndex];
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).TotalIva += arrValoresRecargo[lngIndex]; //BID 533409
						}
					}
					lngIndex++;
				}

				//UPGRADE_NOTE: (1061) Erase was upgraded to System.Array.Clear. More Information: http://www.vbtonet.com/ewis/ewi1061.aspx
				if (arrLinhas != null)
				{
					Array.Clear(arrLinhas, 0, arrLinhas.Length);
				}
				arrLinhas = new int[11];
				//UPGRADE_NOTE: (1061) Erase was upgraded to System.Array.Clear. More Information: http://www.vbtonet.com/ewis/ewi1061.aspx
				if (arrValoresIva != null)
				{
					Array.Clear(arrValoresIva, 0, arrValoresIva.Length);
				}
				arrValoresIva = new double[11];

				//BID 523420
				if (blnCalculaTotaisVersaoAnterior)
				{
					if (TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.V6_TotalIvaNaoDedutivel(lngApt) != 0)
					{
						// Array com as diferenças de arredondamento do IVA
						TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.V6_ValoresDifIvaNaoDedutivel(lngApt, ref arrLinhas[0], ref arrValoresIva[0]);
						lngIndex = 0;
						while (arrLinhas[lngIndex] > 0)
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).IvaNaoDedutivel += arrValoresIva[lngIndex];
							lngIndex++;
						}
					}
				}
				else
				{
					//Fim 523420
					if (TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIvaNaoDedutivel(lngApt) != 0)
					{
						// Array com as diferenças de arredondamento do IVA
						TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ValoresDifIvaNaoDedutivel(lngApt, ref arrLinhas[0], ref arrValoresIva[0]);
						lngIndex = 0;
						while (arrLinhas[lngIndex] > 0)
						{
							clsDocumentoVenda.Linhas.GetEdita(arrLinhas[lngIndex]).IvaNaoDedutivel += arrValoresIva[lngIndex];
							lngIndex++;
						}
						//BID 585223
						int tempForVar = clsDocumentoVenda.Linhas.NumItens;
						for (lngIndex = 1; lngIndex <= tempForVar; lngIndex++)
						{
							if (Math.Abs(clsDocumentoVenda.Linhas.GetEdita(lngIndex).IvaNaoDedutivel) > Math.Abs(clsDocumentoVenda.Linhas.GetEdita(lngIndex).TotalIva))
							{
								clsDocumentoVenda.Linhas.GetEdita(lngIndex).IvaNaoDedutivel = clsDocumentoVenda.Linhas.GetEdita(lngIndex).TotalIva;
							}
						}
						//Fim 585223
					}
				} //BID 523420

				//CS.3879 - Imposto de selo
				clsDocumentoVenda.ResumoIS = PreencheResumoIS(clsDocumentoVenda, dblTotalIS);

				if (StringsHelper.ToDoubleSafe(clsDocumentoVenda.RegimeIva) != 1)
				{ //IVA excluído
					//BID 523420
					if (blnCalculaTotaisVersaoAnterior)
					{
						clsDocumentoVenda.TotalMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalMercServ(lngApt);
						clsDocumentoVenda.TotalEcotaxa = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalEcovalor(lngApt);
						clsDocumentoVenda.TotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalIva(lngApt);
						clsDocumentoVenda.TotalDesc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalDescontos(lngApt);
						clsDocumentoVenda.TotalOutros = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalAPagar(lngApt) - (clsDocumentoVenda.TotalMerc - clsDocumentoVenda.TotalDesc + clsDocumentoVenda.TotalIva + clsDocumentoVenda.TotalEcotaxa);
						clsDocumentoVenda.TotalDocumento = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalAPagar(lngApt);
					}
					else
					{
						//Fim 523420
						clsDocumentoVenda.TotalMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalMercServ(lngApt);
						clsDocumentoVenda.TotalEcotaxa = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalEcovalor(lngApt);
						clsDocumentoVenda.TotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIva(lngApt);
						clsDocumentoVenda.TotalRecargo = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalRecargo(lngApt);
						clsDocumentoVenda.TotalDesc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalDescontos(lngApt);
						//CS.242_7.50_Alfa8 - IEC
						clsDocumentoVenda.TotalIEC = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIEC(lngApt);
						//CS.242_7.50_Alfa8 - IEC - Adicionado o "TotalIEC"
						clsDocumentoVenda.TotalOutros = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalAPagar(lngApt) - (clsDocumentoVenda.TotalMerc - clsDocumentoVenda.TotalDesc + clsDocumentoVenda.TotalIva + clsDocumentoVenda.TotalEcotaxa + clsDocumentoVenda.TotalIEC);
						//CS.3879 - Imposto de selo
						clsDocumentoVenda.TotalDocumento = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalAPagar(lngApt) + dblTotalIS;
						clsDocumentoVenda.TotalIS = dblTotalIS;

					} //BID 523420
				}
				else
				{
					// Seccao com iva incluído retirar ao TotalMerc e TotalOutros o valor do iva
					// Os preços têm que ser gravados na base de dados sem iva

					//BID 523420
					if (blnCalculaTotaisVersaoAnterior)
					{
						clsDocumentoVenda.TotalDocumento = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalAPagar(lngApt);
						clsDocumentoVenda.TotalEcotaxa = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalEcovalor(lngApt);
						dblTotalIvaMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalIvaTodosArtigos(lngApt);
						clsDocumentoVenda.TotalMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalMercServ(lngApt) - dblTotalIvaMerc;
						clsDocumentoVenda.TotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalIva(lngApt);
						clsDocumentoVenda.TotalDesc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_TotalDescontos(lngApt);
					}
					else
					{
						//Fim 523420

						clsDocumentoVenda.TotalEcotaxa = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalEcovalor(lngApt);
						dblTotalIvaMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIvaTodosArtigos(lngApt);
						//BID 570987 (foi adicionado "- V10_TotalRecargo(lngApt)")
						clsDocumentoVenda.TotalMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalMercServ(lngApt) - dblTotalIvaMerc - TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalRecargo(lngApt);
						clsDocumentoVenda.TotalRecargo = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalRecargo(lngApt);
						clsDocumentoVenda.TotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIva(lngApt);
						clsDocumentoVenda.TotalDesc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalDescontos(lngApt);
						//CS.242_7.50_Alfa8 - IEC
						clsDocumentoVenda.TotalIEC = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalIEC(lngApt);
						//CS.3879 - Imposto de selo
						//.TotalDocumento = V10_TotalAPagar(lngApt)
						clsDocumentoVenda.TotalDocumento = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_TotalAPagar(lngApt) + dblTotalIS;
						clsDocumentoVenda.TotalIS = dblTotalIS;
					} //BID 523420

					//CS.242_7.50_Alfa8 - IEC - Adicionado o "TotalIEC"
					if (TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalDocumento, 10) != TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalMerc + clsDocumentoVenda.TotalIva - clsDocumentoVenda.TotalDesc + clsDocumentoVenda.TotalEcotaxa + clsDocumentoVenda.TotalIEC + clsDocumentoVenda.TotalIS, 10))
					{ // Existem "Outros"
						clsDocumentoVenda.TotalOutros = clsDocumentoVenda.TotalDocumento - (clsDocumentoVenda.TotalMerc + clsDocumentoVenda.TotalIva - clsDocumentoVenda.TotalDesc + clsDocumentoVenda.TotalEcotaxa + clsDocumentoVenda.TotalIEC + clsDocumentoVenda.TotalIS); // c/IVA
					}
					else
					{
						clsDocumentoVenda.TotalOutros = 0;
					}

				}

				if (!blnCalculaTotaisVersaoAnterior)
				{ //BID 523420
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ResumoIVADocumento(lngApt, ref arrResumo[0], ref arrCodsIva);
				} //BID 523420


				//Calcula o resumo do iva
				clsDocumentoVenda.ResumoIva = CalculaResumoIva(clsDocumentoVenda, arrResumo, arrCodsIva, objColIvaNDedutivel);

				if (blnCalculaTotaisVersaoAnterior)
				{
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc.v6_LibertaTotais(lngApt);
				}
				else
				{
					TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_LibertaTotais(lngApt);
				}

				//Arredonda os valores (Corrige o erro dos DOUBLE)
				//BID 586324 (o arredondamento foi alterado de 10 para 9 casas decimais)
				clsDocumentoVenda.TotalDesc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalDesc, 9);
				clsDocumentoVenda.TotalDocumento = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalDocumento, 9);
				clsDocumentoVenda.TotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalIva, 9);
				clsDocumentoVenda.TotalRecargo = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalRecargo, 9);
				clsDocumentoVenda.TotalMerc = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalMerc, 9);
				clsDocumentoVenda.TotalOutros = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalOutros, 9);
				clsDocumentoVenda.TotalEcotaxa = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalEcotaxa, 9);
				//CS.242_7.50_Alfa8 - IEC
				clsDocumentoVenda.TotalIEC = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalIEC, 9);
				//CS.3879
				clsDocumentoVenda.TotalIS = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsDocumentoVenda.TotalIS, 9);

				//CS.3405 - Adiantamentos em CC - Calcula as diferenças de arredondamento
				FuncoesComuns100.FuncoesBS.Documentos.CalculaDiferencas(clsDocumentoVenda, ConstantesPrimavera100.Modulos.Vendas);

				//User Story 4769:Como utilizador quero ter uma coluna com os descontos rateados das linhas especiais de desconto
				FuncoesComuns100.FuncoesBS.Documentos.RateiaDescontosEmValor(clsDocumentoVenda);

				objColIvaNDedutivel = null;
			}
			catch (System.Exception excep)
			{

				objColIvaNDedutivel = null;
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.CalculaValoresTotais", excep.Message);
			}

		}

		private BasBEResumoIvas CalculaResumoIva(VndBE100.VndBEDocumentoVenda DocumentoVenda, double[] arrResumo, TestPrimaveraDarwinSupport.PInvoke.UnsafeNative.Structures.StructCodigosIva arrCodsIva, OrderedDictionary objColIvaNDedutivel)
		{
			BasBEResumoIvas result = null;
			BasBEResumoIva objResumoIVA = null;
			bool blnDeduzLiquidaIVA = false;
			string strCodIva = "";

			try
			{

				result = new BasBEResumoIvas();


				string tempRefParam = DocumentoVenda.Tipodoc;
				string tempRefParam2 = "DeduzLiquidaIVA";
				blnDeduzLiquidaIVA = m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam, tempRefParam2));

				//CS.3995 - Iva de Caixa
				if (blnDeduzLiquidaIVA && FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{

					blnDeduzLiquidaIVA = Convert.ToBoolean(m_objErpBSO.Contabilidade.ExerciciosCBL.TrataIvaCaixa(DocumentoVenda.DataDoc.Year, DocumentoVenda.DataDoc.Month));

				}

				for (int intTaxa = 1; intTaxa <= LogPRIAPIs.NUM_TAXAS_IVA; intTaxa++)
				{

					if (LogPRIAPIs.CodIvaResumoPreenchido(ref strCodIva, intTaxa, arrCodsIva))
					{

						objResumoIVA = new BasBEResumoIva();

						objResumoIVA.ID = DocumentoVenda.ID;
						objResumoIVA.Modulo = ConstantesPrimavera100.Modulos.Vendas;
						objResumoIVA.Filial = DocumentoVenda.Filial;
						objResumoIVA.Tipodoc = DocumentoVenda.Tipodoc;
						objResumoIVA.Serie = DocumentoVenda.Serie;
						objResumoIVA.NumDoc = DocumentoVenda.NumDoc;

						objResumoIVA.CodIva = strCodIva;
						objResumoIVA.TaxaIva = arrResumo[intTaxa * 5 - 4];

						//BID 585342 (foi adicionado o "Arredonda" para corrigir o problema de arredondamento dos DOUBLE)
						//BID 598359
						objResumoIVA.Incidencia = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(arrResumo[intTaxa * 5 - 3], 9);
						objResumoIVA.Valor = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(arrResumo[intTaxa * 5 - 2], 9);
						//^BID 598359

						objResumoIVA.TaxaRecargo = arrResumo[intTaxa * 5 - 1];
						objResumoIVA.ValorRecargo = arrResumo[intTaxa * 5];

						//CS.3995 - Iva de Caixa
						if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objResumoIVA.CodIva, objColIvaNDedutivel))
						{

							objResumoIVA.IvaNaoDedutivel = (double) objColIvaNDedutivel[objResumoIVA.CodIva];

						}
						else
						{

							objResumoIVA.IvaNaoDedutivel = 0;

						}

						if (DocumentoVenda.RegimeIva == ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva).ToString())
						{

							objResumoIVA.TaxaIva = 0;
							objResumoIVA.Valor = 0;
							objResumoIVA.TaxaRecargo = 0;
							objResumoIVA.ValorRecargo = 0;

						}

						objResumoIVA.IVAIndeferido = blnDeduzLiquidaIVA;
						objResumoIVA.IDOrig = "{00000000-0000-0000-0000-000000000000}";

						result.Insere(objResumoIVA);

						objResumoIVA = null;

					}

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_CalculaResumoIva", excep.Message);
			}


			return result;
		}


		//CS.3693 (foi adicionado o parâmetro "Versao")
		private void CalculaValoresLinhas(VndBE100.VndBELinhaDocumentoVenda ObjLinha, double dblDescCli, double dblDescPag, int intRegimeIva, int intArredondamento, int intArredondaIva, bool bolSujeitoRecargo, string strVersao, string MovStock)
		{

			int lngApt = 0;
			double dblDesconto = 0;
			double[] ArrValores = new double[12];

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (Erro)");

			lngApt = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_AlocaTotais();


			TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_InicializaTotais(lngApt, dblDescCli, dblDescPag, (short) intRegimeIva, 1, (short) intArredondamento, (short) intArredondaIva, 0, (bolSujeitoRecargo) ? ((short) (-1)) : ((short) 0));

			dblDesconto = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaDescontoTotal(ObjLinha.Desconto1, ObjLinha.Desconto2, ObjLinha.Desconto3);

			if (Information.IsNumeric(ObjLinha.TipoLinha))
			{
				//CS.242_7.50_Alfa8 - Adicionado o "ValorIEC"
				//BID 557608 (foi adicionado o "Arredonda(...,2)" à taxa de Iva)
				//CS.3693 (foi adicionado o parâmetro "Versao")
				//BID 591151 : foi adicionada a multiplicação do Factor de Conversão ao valor da Ecotaxa
				if (strVersao == "08.02" || String.CompareOrdinal(strVersao, "09.01") >= 0)
				{
					LogPRIAPIs.V10_InsereLinha(lngApt, Convert.ToInt32(Double.Parse(ObjLinha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ObjLinha.TaxaIva, 2), ObjLinha.PrecUnit, dblDesconto, ObjLinha.Quantidade, ObjLinha.CodIva, 0, (ObjLinha.SujeitoRetencao) ? -1 : 0, ObjLinha.TaxaRecargo, ObjLinha.PercIncidenciaIVA, 1, ObjLinha.PercIvaDedutivel, 100, ObjLinha.Ecotaxa * ObjLinha.FactorConv, ObjLinha.CodIvaEcotaxa, ObjLinha.TaxaIvaEcotaxa, Convert.ToInt32(ObjLinha.IvaRegraCalculo), ObjLinha.ValorIEC, ObjLinha.BaseCalculoIncidencia, (int) ObjLinha.RegraCalculoIncidencia, FuncoesComuns100.FuncoesBS.Documentos.IgnoraCalculoMargem(ConstantesPrimavera100.Modulos.Vendas, MovStock, ObjLinha.Quantidade, ObjLinha.RegraCalculoIncidencia), strVersao);
				}
				else
				{
					//Fim 591151
					LogPRIAPIs.V10_InsereLinha(lngApt, Convert.ToInt32(Double.Parse(ObjLinha.TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ObjLinha.TaxaIva, 2), ObjLinha.PrecUnit, dblDesconto, ObjLinha.Quantidade, ObjLinha.CodIva, 0, (ObjLinha.SujeitoRetencao) ? -1 : 0, ObjLinha.TaxaRecargo, ObjLinha.PercIncidenciaIVA, 1, ObjLinha.PercIvaDedutivel, 100, ObjLinha.Ecotaxa, ObjLinha.CodIvaEcotaxa, ObjLinha.TaxaIvaEcotaxa, Convert.ToInt32(ObjLinha.IvaRegraCalculo), ObjLinha.ValorIEC, ObjLinha.BaseCalculoIncidencia, (int) ObjLinha.RegraCalculoIncidencia, FuncoesComuns100.FuncoesBS.Documentos.IgnoraCalculoMargem(ConstantesPrimavera100.Modulos.Vendas, MovStock, ObjLinha.Quantidade, ObjLinha.RegraCalculoIncidencia), strVersao);
				}
			}

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (ERRO_SECCAO)");

			TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ValoresUltimaLinha(lngApt, ref ArrValores[0]);

			ArrValores[4] = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(ArrValores[4], (short) intArredondamento); //BID 585250

			ObjLinha.TotalIliquido = ArrValores[0];
			ObjLinha.TotalDA = ArrValores[1];
			ObjLinha.TotalDC = ArrValores[2];
			ObjLinha.TotalDF = ArrValores[3];
			ObjLinha.DescontoComercial = ObjLinha.TotalDA + ObjLinha.TotalDC;
			ObjLinha.PrecoLiquido = ArrValores[4];
			ObjLinha.TotalIva = ArrValores[6] + ArrValores[7];
			ObjLinha.TotalRecargo = ArrValores[7];
			ObjLinha.IvaNaoDedutivel = ArrValores[8];
			ObjLinha.TotalEcotaxa = ArrValores[9];
			//CS.242_7.50_Alfa8
			ObjLinha.TotalIEC = ArrValores[10];
			ObjLinha.BaseIncidencia = ArrValores[11];


Continua:
			TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_LibertaTotais(lngApt);

			return;

ERRO_SECCAO:
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			if (Information.Err().Number == 3078)
			{
				goto Continua;
			}

Erro:
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.CalculaValoresLinhas", Information.Err().Description);

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_EditaRascunhoID
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : Id -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public VndBEDocumentoVenda EditaRascunhoID(string Id)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.EditaRascunhoID(ref Id);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_EditaRascunhoID", excep.Message);
			}
			return null;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ExisteRascunhoID
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : Id -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool ExisteRascunhoID(string Id)
		{

			return m_objErpBSO.DSO.Vendas.Documentos.ExisteRascunhoID(ref Id);

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_LstDocumentosRascunho
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : TipoDoc    -->
		// Arguments   : Serie      -->
		// Arguments   : Filial     -->
		// Arguments   : Utilizador -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBELista LstDocumentosRascunho( string TipoDoc,  string Serie,  string Filial,  string Utilizador)
		{

			return m_objErpBSO.DSO.Vendas.Documentos.LstDocumentosRascunho( TipoDoc,  Serie,  Filial,  Utilizador);

		}

		public StdBELista LstDocumentosRascunho( string TipoDoc,  string Serie,  string Filial)
		{
			string tempRefParam125 = "";
			return LstDocumentosRascunho( TipoDoc,  Serie,  Filial,  tempRefParam125);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_PreencheRegimeIva
		// Description : CS.1398 - Preenche o regime de iva, dado o Espaco fiscal e RegimeIvaReembolsos
		// Arguments   : DocumentoVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public VndBEDocumentoVenda PreencheRegimeIva(VndBEDocumentoVenda DocumentoVenda)
		{
			VndBEDocumentoVenda result = null;
			string StrErro = "";
			string strEspacofiscal = "";
			int intEspacoFiscal = 0;
			VndBE100.VndBEDocumentoVenda objDocFinal = null;

			try
			{

				intEspacoFiscal = DocumentoVenda.EspacoFiscal;

				//Para ficar igual ao espaço fiscal das entidades
				if (intEspacoFiscal >= 1)
				{

					intEspacoFiscal--;

				}

				strEspacofiscal = m_objErpBSO.DSO.Plat.Utils.FStr(intEspacoFiscal);

				objDocFinal = DocumentoVenda;

				//Recebe o documento e preenche o campo Regime de Iva

				//Calcula o regime de IVA
				objDocFinal.RegimeIva = m_objErpBSO.DSO.Plat.Utils.FStr((int) FuncoesComuns100.FuncoesBS.Documentos.DevolveEspacoFiscalCalculado(strEspacofiscal, objDocFinal.RegimeIvaReembolsos, m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, objDocFinal.Tipodoc, objDocFinal.Serie, "IvaIncluido")), ConstantesPrimavera100.Modulos.Vendas));


				//No final valida se os dados estão correctos
				if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaRegimeIvaEditores(objDocFinal, ConstantesPrimavera100.Modulos.Vendas, ref StrErro))
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IVndBSVendas_PreencheRegimeIva", StrErro);

				}

				result = objDocFinal;

				objDocFinal = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_PreencheRegimeIva", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_RemoveRascunhoID
		// Description : Hades CS.1577 - Rascunho nos Documentos de Venda
		// Arguments   : ID --> Identificador do rascunho
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void RemoveRascunhoID(string Id)
		{

			try
			{

				m_objErpBSO.DSO.Vendas.Documentos.RemoveRascunhoID(ref Id);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_RemoveRascunhoID", excep.Message);
			}

		}

		public void SugerePrecoDesconto(System.DateTime DataDoc, string Moeda, string Cliente, string Artigo, string Contrato, string Unidade, double Quantidade, double FactorConversao, double PrecoUnit, double PrecoSugerido, bool IvaIncluido, double TaxaIva, double DescontoSugerido, double DescontoSugerido1, double DescontoSugerido2, double DescontoSugerido3, bool SugereSemRegras, bool SugerePreco, bool SugereDesc, bool SoComEscaloes)
		{
			double dblFactConv = 0;
			bool bolIvaIncluido = false;
			double dblTaxaIva = 0;
			string strUnidadeBase = "";

			string strUnidade = Unidade;

			if ((m_objErpBSO.Vendas.Licenca.Vendas.RegrasDescontosPrecos) || m_objErpBSO.Base.Licenca.ConfiguracaoStarterEasy)
			{

				m_objErpBSO.Vendas.DescontosPrecos.SugerePrecoDesconto(DataDoc, Moeda, Cliente, Artigo, Contrato, strUnidade, Quantidade, PrecoUnit, PrecoSugerido, bolIvaIncluido, dblTaxaIva, DescontoSugerido, DescontoSugerido1, DescontoSugerido2, DescontoSugerido3, SugereSemRegras, SugerePreco, SugereDesc, SoComEscaloes);

				//BID:594244 - Por omissão recebe a taxa que é passada por parametro, porque na sugestão das regras não recebe a taxa
				dblTaxaIva = TaxaIva;

			}
			else
			{
				// Ignorar regras, devolve -1
				PrecoSugerido = -1;
				DescontoSugerido = -1;
				DescontoSugerido1 = -1;
				DescontoSugerido2 = -1;
				DescontoSugerido3 = -1;
			}

			if (Strings.Len(strUnidade) == 0)
			{
				strUnidade = Unidade;
			}

			if (SugerePreco && PrecoSugerido != -1)
			{
				//foi sugerido um preço
				if (Unidade != strUnidade)
				{ //encontrou uma regra mas numa unidade diferente da solicitada
					//factor de conversão entre a nova unidade e a unidade base

					strUnidadeBase = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "UnidadeBase")).Trim();
					if (strUnidade != strUnidadeBase)
					{
						dblFactConv = m_objErpBSO.Base.Artigos.FactorConvUnOrigUnDest(ref Artigo, ref strUnidade, ref strUnidadeBase);
						if (dblFactConv != 0)
						{
							//converte preço para a unidade base
							PrecoSugerido *= dblFactConv;
						}
					}

					//converte da unidade base para a unidade introduzida
					PrecoSugerido *= FactorConversao;
				}
				//TaxaIva = dblTaxaIva 'BID 523126/523499/524506
			}

			if (!SoComEscaloes)
			{ // Se seleccionar "SoComEscaloes" não sugere as condições normais
				if (SugereSemRegras)
				{
					if (SugerePreco && PrecoSugerido == -1)
					{ // A regra não sugeriu o preço
						if (m_objErpBSO.Base.ArtigosPrecos.Existe(ref Artigo, ref Moeda, ref Unidade))
						{
							string tempRefParam = m_objErpBSO.Base.Clientes.DaPrecoCliente(Cliente);
							PrecoSugerido = m_objErpBSO.Base.ArtigosPrecos.DaPrecoArtigoMoeda(ref Artigo, ref Moeda, ref Unidade, ref tempRefParam, ref bolIvaIncluido, ref dblTaxaIva);
							//If PrecoSugerido = 0 Then PrecoSugerido = -1 'BID 534020
						}
						else
						{
							if (PrecoUnit == 0 || PrecoUnit == -1)
							{ //BID 527508
								strUnidadeBase = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "UnidadeBase")).Trim();
								string tempRefParam2 = m_objErpBSO.Base.Clientes.DaPrecoCliente(Cliente);
								PrecoSugerido = m_objErpBSO.Base.ArtigosPrecos.DaPrecoArtigoMoeda(ref Artigo, ref Moeda, ref strUnidadeBase, ref tempRefParam2, ref bolIvaIncluido, ref dblTaxaIva);
								PrecoSugerido *= FactorConversao;
								//BID 527508
							}
							else
							{
								//BID:547508 - Só faz o calculo da Conversão, se o preço tiver sido calculado
								if (PrecoUnit != -1)
								{

									PrecoSugerido = PrecoUnit * FactorConversao;

								}
								//END BID:547508

								bolIvaIncluido = IvaIncluido; //BID 543994
							}
							//Fim 527508
						}
					}
					if (DescontoSugerido1 == -1 && SugereDesc)
					{ // A regra não sugeriu o desconto
						DescontoSugerido = ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "Desconto"));
						if (DescontoSugerido == 0)
						{
							DescontoSugerido = -1;
						}
						DescontoSugerido1 = DescontoSugerido;
						DescontoSugerido2 = 0;
						DescontoSugerido3 = 0;
					}
				}
			}

			if (PrecoSugerido != -1)
			{

				if ((IvaIncluido || bolIvaIncluido) && PrecoSugerido != -1)
				{

					//BID 593523/594214
					//PrecoSugerido = m_objErpBSO.Base.Artigos.ConvertePreco(PrecoSugerido, bolIvaIncluido, TaxaIva, IvaIncluido, TaxaIva, m_objErpBSO.Base.Moedas.DaValorAtributo(Moeda, "DecPrecUnit"))
					if (bolIvaIncluido)
					{
						//Se o preço tem IVA incluído, deve ser passada a taxa de IVA do artigo no parâmetro [TaxaIvaOrig]
						PrecoSugerido = m_objErpBSO.Base.Artigos.ConvertePreco(PrecoSugerido, bolIvaIncluido, dblTaxaIva, IvaIncluido, TaxaIva, ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Moedas.DaValorAtributo(Moeda, "DecPrecUnit")));
					}
					else
					{
						PrecoSugerido = m_objErpBSO.Base.Artigos.ConvertePreco(PrecoSugerido, bolIvaIncluido, TaxaIva, IvaIncluido, TaxaIva, ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Moedas.DaValorAtributo(Moeda, "DecPrecUnit")));
					}
					//Fim 593523/594214

				}
				else
				{

					PrecoSugerido = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(PrecoSugerido, ReflectionHelper.GetPrimitiveValue<short>(m_objErpBSO.Base.Moedas.DaValorAtributo(Moeda, "DecPrecUnit"))); //BID 594621

				}

			}

			//BID 525270 - DA
			SugerePrecoDescontoUserExit(DataDoc, Moeda, Cliente, Artigo, Contrato, Unidade, Quantidade, FactorConversao, PrecoUnit, PrecoSugerido, IvaIncluido, TaxaIva, DescontoSugerido, DescontoSugerido1, DescontoSugerido2, DescontoSugerido3, SugereSemRegras, SugerePreco, SugereDesc, SoComEscaloes);
		}

		public void SugerePrecoDesconto( System.DateTime DataDoc,  string Moeda,  string Cliente,  string Artigo,  string Contrato,  string Unidade,  double Quantidade,  double FactorConversao,  double PrecoUnit,  double PrecoSugerido,  bool IvaIncluido,  double TaxaIva,  double DescontoSugerido,  double DescontoSugerido1,  double DescontoSugerido2,  double DescontoSugerido3,  bool SugereSemRegras,  bool SugerePreco,  bool SugereDesc)
		{
			bool tempRefParam126 = false;
			SugerePrecoDesconto( DataDoc,  Moeda,  Cliente,  Artigo,  Contrato,  Unidade,  Quantidade,  FactorConversao,  PrecoUnit,  PrecoSugerido,  IvaIncluido,  TaxaIva,  DescontoSugerido,  DescontoSugerido1,  DescontoSugerido2,  DescontoSugerido3,  SugereSemRegras,  SugerePreco,  SugereDesc,  tempRefParam126);
		}

		public void SugerePrecoDesconto( System.DateTime DataDoc,  string Moeda,  string Cliente,  string Artigo,  string Contrato,  string Unidade,  double Quantidade,  double FactorConversao,  double PrecoUnit,  double PrecoSugerido,  bool IvaIncluido,  double TaxaIva,  double DescontoSugerido,  double DescontoSugerido1,  double DescontoSugerido2,  double DescontoSugerido3,  bool SugereSemRegras,  bool SugerePreco)
		{
			bool tempRefParam127 = true;
			bool tempRefParam128 = false;
			SugerePrecoDesconto( DataDoc,  Moeda,  Cliente,  Artigo,  Contrato,  Unidade,  Quantidade,  FactorConversao,  PrecoUnit,  PrecoSugerido,  IvaIncluido,  TaxaIva,  DescontoSugerido,  DescontoSugerido1,  DescontoSugerido2,  DescontoSugerido3,  SugereSemRegras,  SugerePreco,  tempRefParam127,  tempRefParam128);
		}

		public void SugerePrecoDesconto( System.DateTime DataDoc,  string Moeda,  string Cliente,  string Artigo,  string Contrato,  string Unidade,  double Quantidade,  double FactorConversao,  double PrecoUnit,  double PrecoSugerido,  bool IvaIncluido,  double TaxaIva,  double DescontoSugerido,  double DescontoSugerido1,  double DescontoSugerido2,  double DescontoSugerido3,  bool SugereSemRegras)
		{
			bool tempRefParam129 = true;
			bool tempRefParam130 = true;
			bool tempRefParam131 = false;
			SugerePrecoDesconto( DataDoc,  Moeda,  Cliente,  Artigo,  Contrato,  Unidade,  Quantidade,  FactorConversao,  PrecoUnit,  PrecoSugerido,  IvaIncluido,  TaxaIva,  DescontoSugerido,  DescontoSugerido1,  DescontoSugerido2,  DescontoSugerido3,  SugereSemRegras,  tempRefParam129,  tempRefParam130,  tempRefParam131);
		}

		public void SugerePrecoDesconto( System.DateTime DataDoc,  string Moeda,  string Cliente,  string Artigo,  string Contrato,  string Unidade,  double Quantidade,  double FactorConversao,  double PrecoUnit,  double PrecoSugerido,  bool IvaIncluido,  double TaxaIva,  double DescontoSugerido,  double DescontoSugerido1,  double DescontoSugerido2,  double DescontoSugerido3)
		{
			bool tempRefParam132 = true;
			bool tempRefParam133 = true;
			bool tempRefParam134 = true;
			bool tempRefParam135 = false;
			SugerePrecoDesconto( DataDoc,  Moeda,  Cliente,  Artigo,  Contrato,  Unidade,  Quantidade,  FactorConversao,  PrecoUnit,  PrecoSugerido,  IvaIncluido,  TaxaIva,  DescontoSugerido,  DescontoSugerido1,  DescontoSugerido2,  DescontoSugerido3,  tempRefParam132,  tempRefParam133,  tempRefParam134,  tempRefParam135);
		}

		public bool ValidaActualizacao(VndBEDocumentoVenda clsDocumentoVenda, VndBETabVenda clsTabVenda, string SerieDocLiq, string StrErro, BasBESerie clsSerie)
		{

			return ValidaActualizacao(clsDocumentoVenda, clsTabVenda, SerieDocLiq, StrErro, clsSerie);

		}

		//TUNNING
		private bool ValidaActualizacao(VndBEDocumentoVenda clsDocumentoVenda, VndBETabVenda clsTabVenda, string SerieDocLiq, string StrErro, BasBESerie clsSerie, bool DocTrataTransacaoElectronica, string Avisos)
		{
			//##SUMMARY Efectua todas as validações necessárias à gravação de um documento de venda.
			//##PARAM clsDocumentoVenda Objecto que identifica o documento de venda a validar.
			//##PARAM clsTabVenda Objecto que identifica o tipo de documento de stock.
			//##PARAM StrErro Descrição do erro devolvida pela função.
			bool result = false;
			double dblValorTotal = 0;
			double dblValor = 0;
			string strDocumento = "";
			System.DateTime datDataInicio = DateTime.FromOADate(0);
			System.DateTime datDataFim = DateTime.FromOADate(0);
			int intCasasDecimais = 0;
			BasBESerie objSerie = null;
			int varVersao = 0;
			bool blnExisteEntidadeAssociada = false;
			bool blnEntidadeFacturacao = false;
			bool blnMesmaEntidade = false;
			StdBE100.StdBECampos objCampos = null; //BID 564775
			double dblTotalMoedaBase = 0;
			bool blnSATFTipoDocInvalido = false;
			string strErroATDocCodeID = "";
			double dblTotalAdiantamentos = 0; //CS.3405 - Adiantamentos em CC
			bool blnActEmpresarial = false;
			bool blnTTEEditavel = false;
			bool blnDocTransfEstornado = false;
			int intNumCasasDecMoedaBase = 0;
			string strMensagemErro = ""; //BID 593536
			string strXMLLinhasValidarNecessidade = "";
			string strSQL = "";
			bool blnUltimoDocumentoVenda = false; //BID 17369



			try
			{

				result = true;

				string tempRefParam = "CabecDoc";
				if (m_objErpBSO.Base.VersaoDemoExcedida(ref tempRefParam))
				{ //PriGlobal: IGNORE
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9065, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9066, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					return result;
				}

				//Em empresas produtivas e com NIF Dummy não é possivel lancar este tipo de documentos
				if (!clsDocumentoVenda.EmModoEdicao)
				{

					if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaLancamentoDocumentos(ref StrErro))
					{

						return false;

					}

				}
				else
				{

					//Se o documento não está em edição vamos verificar se tem a informação atualizada
					strSQL = "SELECT Id FROM CabecDoc (NOLOCK) WHERE Id = '@1@' AND Desatualizado = 1";
					dynamic[] tempRefParam2 = new dynamic[]{clsDocumentoVenda.ID};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam2);
					if (!m_objErpBSO.Consulta(strSQL).Vazia())
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18289, ConstantesPrimavera100.AbreviaturasApl.Internos);
						return result;

					}

				}

				if (clsDocumentoVenda.TotalDocumento < 0 && (!clsTabVenda.PermiteDocNegativo))
				{ //CR.141 - Só se o documento estiver parametrizado para permitir docs negativos
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9065, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9573, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					return result;
				}

				//CS.3421 - LOG: Controlo dos documentos emitidos
				//Não podem ser criados novos documentos, quando os documentos se encontram Inactivos
				if (clsTabVenda.Inactivo && !clsDocumentoVenda.EmModoEdicao)
				{

					result = false;
					string tempRefParam3 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16354, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam4 = new dynamic[]{clsTabVenda.Documento};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam3, tempRefParam4) + Environment.NewLine;

				}

				if (!CertificacaoSoftware.ValidaCamposSAFT(clsDocumentoVenda, ref StrErro))
				{

					result = false;

				}

				if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{
					// Se designação fiscal for do tipo DC a partir do 01-07-2017, tem de dar erro
					if (clsTabVenda.SAFTTipoDoc == "DC" && clsDocumentoVenda.DataDoc >= DateTime.Parse("2017-07-01"))
					{ //PriGlobal: IGNORE

						//"Designação fiscal DC (Documentos Conferência ou Prestação de Serviços) não é permitida a partir de 01-07-2017"
						result = false;
						string tempRefParam5 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18624, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam6 = new dynamic[]{clsTabVenda.Documento};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam5, tempRefParam6);

					}

					//CS.3421 - LOG: Controlo dos documentos emitidos
					//Se tem um limite definido na ficha não pode ultrapassar esse limite em valor (na moeda base)
					if (clsTabVenda.ValorLimite != 0 && !clsDocumentoVenda.EmModoEdicao)
					{

						intNumCasasDecMoedaBase = m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Base.Moedas.DaValorAtributo(m_objErpBSO.Contexto.MoedaBase, "DecArredonda"));

						dblTotalMoedaBase = clsDocumentoVenda.TotalDocumento - clsDocumentoVenda.TotalIva;

						//Se é diferente da moeda base, então converte o valor para a mesma
						if (clsDocumentoVenda.Moeda != m_objErpBSO.Contexto.MoedaBase)
						{ //PriGlobal: IGNORE

							dblTotalMoedaBase = STDPriAPIDivisas.TransfMOrigMBase(dblTotalMoedaBase, clsDocumentoVenda.Cambio, clsDocumentoVenda.CambioMBase, STDPriAPIDivisas.MOEDASTipoArred.MOEDAS_ArredValor);

						}

						//Se ultrapassa o limite, não deixa gravar
						if (TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblTotalMoedaBase, (short) intNumCasasDecMoedaBase) > TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(clsTabVenda.ValorLimite, (short) intNumCasasDecMoedaBase))
						{

							result = false;
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16355, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						}

					}

				}

				//Valida se as entidades tem actividade empresarial
				if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{

					if (clsDocumentoVenda.TrataIvaCaixa && (clsDocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente))
					{

						blnActEmpresarial = m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Clientes.DaValorAtributo(clsDocumentoVenda.Entidade, "ActividadeEmpresarial"));

					}
					else if (clsDocumentoVenda.TrataIvaCaixa && (clsDocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor))
					{ 

						blnActEmpresarial = m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.OutrosTerceiros.DaValorAtributo(clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidade, "ActividadeEmpresarial"));

					}
					else if ((clsDocumentoVenda.TipoEntidade != ConstantesPrimavera100.TiposEntidade.Cliente))
					{ 

						blnActEmpresarial = false;

					}

					//Clientes sem actividade empresarial, nao podem ter tratamento de Iva de Caixa
					if (!blnActEmpresarial && clsDocumentoVenda.TrataIvaCaixa)
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16767, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}

				if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{

					//CS.3421 - LOG: Controlo dos documentos emitidos
					//Se tem data >= que 01 de Jan de 2013, então não pode gravar, caso seja um documento com designação fiscal (VD, TV ou TD)
					if (clsDocumentoVenda.DataDoc >= ConstantesPrimavera100.Fiscal.DataFacturaSimples && !clsDocumentoVenda.EmModoEdicao)
					{

						blnSATFTipoDocInvalido = false;


						switch(clsTabVenda.SAFTTipoDoc.ToUpper())
						{
							//BID 584279 (foram adicionadas as designações fiscais "AA" e "DA")
							case "VD" : case "TV" : case "TD" : case "AA" : case "DA" :  
								blnSATFTipoDocInvalido = true;  //PriGlobal: IGNORE 
								 
								break;
						}

						//É Invalido??
						if (blnSATFTipoDocInvalido)
						{

							result = false;
							string tempRefParam7 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16356, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam8 = new dynamic[]{clsTabVenda.SAFTTipoDoc.ToUpper()};
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam7, tempRefParam8);
							return result;

						}

					}

				}

				//CS.2143 – Anulação de documentos
				if (Strings.Len(clsDocumentoVenda.ID) > 0)
				{

					if (m_objErpBSO.Vendas.Documentos.DocumentoAnuladoID(clsDocumentoVenda.ID))
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9065, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16009, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);

						return result;

					}

				}
				//END CS.2143

				//CS.3649 – Comunicação de documentos de transporte à AT
				if (clsDocumentoVenda.EmModoEdicao)
				{

					if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
					{

						//BID 583718 (foi adicionado o parâmetro "objCargaDescarga")
						string tempRefParam9 = "";
						if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaActualizacaoATDocCodeID(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.ID, clsDocumentoVenda.ATDocCodeID, ref strErroATDocCodeID, ref tempRefParam9, clsDocumentoVenda.CargaDescarga))
						{

							result = false;
							StrErro = StrErro + strErroATDocCodeID + Environment.NewLine;

						}

						if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaAtualizacaoCAE(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda))
						{

							result = false;

							StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16605, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						}

					}

				}

				//TUNNING
				if (clsSerie == null)
				{
					objSerie = m_objErpBSO.Base.Series.Edita(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie);
				}
				else
				{
					objSerie = clsSerie;
				}

				//Se não é uma série de integração e é de cálculo manual, não é possível continuar
				result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaTotaisDocumentoIntegracao(clsDocumentoVenda, objSerie, ref StrErro);

				//BID 589032 : Foi adicionado o parâmetro "intTipoComunicacao"
				if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaDocsComAT(clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, ConstantesPrimavera100.Modulos.Vendas, ref strErroATDocCodeID, objSerie.TipoComunicacao))
				{

					result = false;
					StrErro = StrErro + strErroATDocCodeID + Environment.NewLine;

				}

				if (m_objErpBSO.Vendas.Licenca.Vendas.Fluxos)
				{

					if (m_objErpBSO.Vendas.FluxosVenda.ExistemFluxos() && Strings.Len(clsDocumentoVenda.Fluxo) == 0)
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15379, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}

				//BID 590327 : Impedir a alteração de documentos contabilizados através de ligação off-line (em Portugal, a certificação já impede alterar documentos)
				if (!FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{

					if ((((clsDocumentoVenda.CBLEstado != 0) ? -1 : 0) & ~(Convert.ToInt32(m_objErpBSO.Contabilidade.ConfiguracaoDocCBL.DaValorAtributo(m_objErpBSO.DSO.Plat.FuncoesGlobais.DaExercicioEconomico(clsDocumentoVenda.DataDoc, (short) m_objErpBSO.Contexto.IFMesInicio), FuncoesComuns100.FuncoesBS.PlanoOficialEAP, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, "LigacaoCBLOnLine")))) != 0)
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13194, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}
				//Fim 590327


				strDocumento = clsDocumentoVenda.Tipodoc + "/" + clsDocumentoVenda.Serie + "/" + clsDocumentoVenda.NumDoc.ToString();

				if (Strings.Len(clsDocumentoVenda.Serie) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9049, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
					return result;
				}

				if (clsTabVenda.RecolhaDE_IL && (Strings.Len(clsDocumentoVenda.DE_IL.Trim()) == 0) && StringsHelper.ToDoubleSafe(clsDocumentoVenda.RegimeIva) == 4)
				{ //Apenas entra se o parâmetro de recolha DE/IL do documento estiver activo, se o número do campo DE/IL for igual a 0 e se o tipo de mercado for externo
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(12619, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				if (Strings.Len(clsDocumentoVenda.Seccao) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9577, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				if (Strings.Len(clsDocumentoVenda.Entidade) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9051, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				if (Strings.Len(clsDocumentoVenda.Moeda) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9055, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					//BID 589222
				}
				else
				{

					if (clsDocumentoVenda.EmModoEdicao)
					{

						string tempRefParam10 = clsDocumentoVenda.Filial;
						string tempRefParam11 = clsDocumentoVenda.Tipodoc;
						string tempRefParam12 = clsDocumentoVenda.Serie;
						int tempRefParam13 = clsDocumentoVenda.NumDoc;
						string tempRefParam14 = "Moeda";
						if (clsDocumentoVenda.Moeda != ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.Documentos.DaValorAtributo(tempRefParam10, tempRefParam11, tempRefParam12, tempRefParam13, tempRefParam14)))
						{

							int tempForVar = clsDocumentoVenda.Linhas.NumItens;
							for (int lngI = 1; lngI <= tempForVar; lngI++)
							{

								if (clsDocumentoVenda.Linhas.GetEdita(lngI).QuantSatisfeita != 0)
								{

									result = false;
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17175, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + " : " + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3066, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
									break;

								}

							}

						}

					}
					//Fim 589222

				}

				//** Verificar se a data do documento não é vazia
				//UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
				if (clsDocumentoVenda.DataDoc == DateTime.FromOADate(0) || clsDocumentoVenda.DataDoc.Equals(DateTime.FromOADate(0)))
				{
					result = false;
					string tempRefParam15 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9585, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam16 = new dynamic[]{strDocumento};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam15, tempRefParam16) + Environment.NewLine;
				}
				else
				{
					//Verificar se as datas do documento estão entre os limtes do exercício
					if (clsDocumentoVenda.DataDoc <= datDataInicio && clsDocumentoVenda.DataDoc >= datDataFim)
					{
						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9586, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
					}
				}

				//** Verificar se a data de vencimento do documento de venda está vazia
				//UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
				if (clsDocumentoVenda.DataVenc == DateTime.FromOADate(0) || clsDocumentoVenda.DataVenc.Equals(DateTime.FromOADate(0)))
				{
					result = false;
					string tempRefParam17 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9587, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam18 = new dynamic[]{strDocumento};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam17, tempRefParam18) + Environment.NewLine;
				}

				//BID 526237
				if (clsTabVenda.LigacaoCC)
				{
					if (clsDocumentoVenda.DataVenc != DateTime.FromOADate(0))
					{
						if (Information.IsDate(clsDocumentoVenda.DataDoc) && Information.IsDate(clsDocumentoVenda.DataVenc))
						{
							if (clsDocumentoVenda.DataVenc < clsDocumentoVenda.DataDoc)
							{
								result = false;
								StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9906, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
							}
						}
					}
				}
				//Fim 526237

				//Validadar o tipo de documento
				if (clsTabVenda.TipoDocumento > 4 && clsTabVenda.TipoDocumento < 0)
				{
					result = false;
					string tempRefParam19 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9590, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam20 = new dynamic[]{clsDocumentoVenda.Tipodoc};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam19, tempRefParam20) + Environment.NewLine;
				}

				if (clsDocumentoVenda.EmModoEdicao)
				{
					if (m_objErpBSO.Base.Filiais.LicencaDeFilial)
					{
						if (!clsTabVenda.PermiteAltAposExp)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.DSO.Base.Filiais.DaAtributoTimesTamp() of type Variant is being forced to int. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							varVersao = ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.DSO.Base.Filiais.DaAtributoTimesTamp("SELECT VersaoUltAct As Versao FROM CabecDoc WITH (NOLOCK) WHERE Filial='" + clsDocumentoVenda.Filial + "' AND Serie='" + clsDocumentoVenda.Serie + "' AND TipoDoc='" + clsDocumentoVenda.Tipodoc + "' AND NumDoc=" + clsDocumentoVenda.NumDoc.ToString())); //PriGlobal: IGNORE
							//FIL A Versão vai ser -1 quando o registo não existir na bd. Solução encontrada para contornar o "problema" da validação
							//ser chamada depois da remoção do documento no Actualiza
							if (varVersao != -1)
							{
								if (ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.DSO.Base.Filiais.TSUltimaExportacao("CabecDoc")) > varVersao)
								{ //PriGlobal: IGNORE
									result = false;
									string tempRefParam21 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9070, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam22 = new dynamic[]{clsDocumentoVenda.Tipodoc};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam21, tempRefParam22) + Environment.NewLine;
								}
							}
						}
					}
				}

				//O Cambio da moeda não pode ser zero
				if (clsDocumentoVenda.Cambio == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9052, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				if (clsDocumentoVenda.CambioMBase == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9053, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				if (clsDocumentoVenda.CambioMAlt == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9054, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				//** Verifica se a condição de pagamento existe
				if (Strings.Len(clsDocumentoVenda.CondPag) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9048, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}

				//** Se o documento tem prestações verificar se a soma das prestacoes é igual ao valor total do documento
				if (clsDocumentoVenda.Prestacoes.NumItens > 1)
				{
					dblValor = Math.Abs(ValorTotalPrestacoes(clsDocumentoVenda.Prestacoes));

					//CS.3405 - Adiantamentos em CC
					if (clsDocumentoVenda.GeraPendentePorLinha)
					{ //BID 589059/589198
						dblTotalAdiantamentos = ValorTotalAdiantamentos(clsDocumentoVenda);
						dblValor = Math.Abs(dblValor - dblTotalAdiantamentos);
					}

					//CS.242_7.50_Alfa8 - Adicionado o "TotalIEC"
					dblValorTotal = Math.Abs(clsDocumentoVenda.TotalDocumento); //CS.3879 - Imposto de selo

					//Porque na realidade os valores apesar de iguais eram diferentes a partir da 10ª... casa decimal

					intCasasDecimais = 2; //BID 521509 (estava ".Arredondamento")

					if (TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblValor, (short) intCasasDecimais) != TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblValorTotal, (short) intCasasDecimais))
					{
						result = false;
						string tempRefParam23 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9599, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam24 = new dynamic[]{dblValor, strDocumento, dblValorTotal};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam23, tempRefParam24) + Environment.NewLine;
					}
				}

				//BID 528377
				if ((clsDocumentoVenda.Entidade == m_objErpBSO.Vendas.Params.ClienteVD) && (!clsTabVenda.ClienteIndiferenciado))
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3866, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}
				//Fim 528377

				//BID 534015
				if (Strings.Len(clsDocumentoVenda.MoradaAlternativaEntrega) > 0 && clsDocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente)
				{
					//BID 596246 : utilizar [.EntidadeEntrega] em vez de [.EntidadeDescarga]
					if (!m_objErpBSO.Base.MoradasAlternativas.Existe(clsDocumentoVenda.TipoEntidade, clsDocumentoVenda.EntidadeEntrega, clsDocumentoVenda.MoradaAlternativaEntrega))
					{
						result = false;
						string tempRefParam25 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14592, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam26 = new dynamic[]{clsDocumentoVenda.MoradaAlternativaEntrega, clsDocumentoVenda.EntidadeEntrega};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam25, tempRefParam26) + Environment.NewLine;
					}
				}
				//Fim 534015

				//CR.680 - 750 Afa7
				if (Strings.Len(clsDocumentoVenda.EntidadeFac) == 0)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14889, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					//BID 562339
				}
				else
				{

					//BID: 576188 - CS.3185

					string switchVar = clsDocumentoVenda.TipoEntidadeFac;
					if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
					{

						if (!m_objErpBSO.Base.Clientes.Existe(clsDocumentoVenda.EntidadeFac))
						{

							result = false;
							string tempRefParam27 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4750, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam28 = new dynamic[]{clsDocumentoVenda.EntidadeFac};
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam27, tempRefParam28) + Environment.NewLine;

						}

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
					{ 

						if (!m_objErpBSO.Base.OutrosTerceiros.Existe(clsDocumentoVenda.EntidadeFac))
						{

							result = false;
							string tempRefParam29 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9726, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam30 = new dynamic[]{clsDocumentoVenda.EntidadeFac};
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam29, tempRefParam30) + Environment.NewLine;

						}

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.EntidadeExterna)
					{ 

						if (~Convert.ToInt32(m_objErpBSO.CRM.EntidadesExternas.Existe(clsDocumentoVenda.EntidadeFac)) != 0)
						{

							result = false;
							string tempRefParam31 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9726, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam32 = new dynamic[]{clsDocumentoVenda.EntidadeFac};
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam31, tempRefParam32) + Environment.NewLine;

						}

					}

					//Fim 562339
				}

				blnExisteEntidadeAssociada = m_objErpBSO.Base.EntidadesAssociadas.Existe(clsDocumentoVenda.TipoEntidade, clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidadeFac, clsDocumentoVenda.EntidadeFac);
				//UPGRADE_WARNING: (1068) m_objErpBSO.Base.EntidadesAssociadas.DaValorAtributo() of type Variant is being forced to bool. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
				blnEntidadeFacturacao = ReflectionHelper.GetPrimitiveValue<bool>(m_objErpBSO.Base.EntidadesAssociadas.DaValorAtributo(clsDocumentoVenda.TipoEntidade, clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidadeFac, clsDocumentoVenda.EntidadeFac, "EntidadeFacturacao"));
				blnExisteEntidadeAssociada = (blnEntidadeFacturacao && blnExisteEntidadeAssociada);

				blnMesmaEntidade = (clsDocumentoVenda.Entidade.Trim() == clsDocumentoVenda.EntidadeFac.Trim());

				if (!blnExisteEntidadeAssociada && !blnMesmaEntidade)
				{
					result = false;
					string tempRefParam33 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14890, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam34 = new dynamic[]{clsDocumentoVenda.EntidadeFac, clsDocumentoVenda.Entidade};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam33, tempRefParam34) + Environment.NewLine;
				}
				//^CR.680 - 750 Afa7

				//BID 588757
				if (!clsDocumentoVenda.EmModoEdicao && Strings.Len(clsDocumentoVenda.Entidade) > 0 && FuncoesComuns100.FuncoesBS.Entidades.DaValorAtributoEntidadeAnulada(clsDocumentoVenda.TipoEntidade, clsDocumentoVenda.Entidade, "ClienteAnulado"))
				{ //PriGlobal: IGNORE
					result = false;
					string tempRefParam35 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9080, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam36 = new dynamic[]{clsDocumentoVenda.Entidade};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam35,tempRefParam36) + Environment.NewLine;
				}
				//Fim 588757

				//BID:536981
				result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaTiposLancamentoTesourariaContasCorr(ref StrErro, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda, clsTabVenda, ref SerieDocLiq);
				//^BID:536981

				//BID: 539579
				if (Strings.Len(clsDocumentoVenda.ContaDomiciliacao) > 0)
				{
					if (!m_objErpBSO.Base.ContasBancariasTerceiros.Existe(clsDocumentoVenda.TipoEntidadeFac, clsDocumentoVenda.EntidadeFac, clsDocumentoVenda.ContaDomiciliacao))
					{
						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15154, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
					}
				}
				//^BID: 539579

				//BID 571351/572715
				string tempRefParam37 = clsDocumentoVenda.Filial;
				string tempRefParam38 = clsDocumentoVenda.Tipodoc;
				string tempRefParam39 = clsDocumentoVenda.Serie;
				int tempRefParam40 = clsDocumentoVenda.NumDoc;
				string tempRefParam41 = "Estado";
				if (ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.Documentos.DaValorAtributo(tempRefParam37, tempRefParam38, tempRefParam39, tempRefParam40, tempRefParam41)) == ConstantesPrimavera100.Documentos.EstadoDocTransformado)
				{
					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16055, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
				}
				//Fim 571351/572715

				//CS.2489
				if (Strings.Len(clsDocumentoVenda.TipoOperacao) > 0)
				{
					string tempRefParam42 = clsDocumentoVenda.TipoOperacao;
					result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoOperacao(ref StrErro, ConstantesPrimavera100.Modulos.Vendas, ref tempRefParam42, clsDocumentoVenda.EspacoFiscal);
					//Se espanha
					if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha && clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro))
					{

						//Tipo Fiscal preenchido
						if (Strings.Len("" + Convert.ToString(m_objErpBSO.Contabilidade.TiposOperacoes.DaValorAtributo(clsDocumentoVenda.TipoOperacao, "TipoFiscal"))) == 0)
						{


							StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18630, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						}

					}

					//Se espanha
				}
				else
				{

					if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha && clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro))
					{

						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18627, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}
				//Fim CS.2489

				//CS:2884 Valida Certificação
				result = result && CertificacaoSoftware.ValidaActualizacao_Assinatura(clsDocumentoVenda, clsTabVenda, ref StrErro, objSerie);

				//CS.1398 - Valida os Regimes de IVA
				FuncoesComuns100.FuncoesBS.Documentos.ValidaRegimeIvaEditores(clsDocumentoVenda, ConstantesPrimavera100.Modulos.Vendas, ref StrErro);

				//TTE - Validação da transação eletrónica
				if (DocTrataTransacaoElectronica)
				{

					if (!ValidaTransElectronica(clsDocumentoVenda, ref StrErro))
					{

						result = false;

					}

					//BID 587571/588035
					if (clsDocumentoVenda.EmModoEdicao && Strings.Len(clsDocumentoVenda.IDDocB2B) != 0)
					{

						blnTTEEditavel = false;
						objCampos = (StdBE100.StdBECampos) m_objErpBSO.TransaccoesElectronicas.B2BTransaccoes.DaValorAtributos(clsDocumentoVenda.IDDocB2B, "Estado", "UltEstadoRec");

						if (objCampos != null)
						{

							string tempRefParam43 = "Estado";
							string tempRefParam44 = "UltEstadoRec";
							string tempRefParam45 = "UltEstadoRec";
							//blnTTEEditavel = (m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.GetItem(ref tempRefParam43)) == ((short) TTEBE100.TTEBEB2BTransacao.TTEB2BEstadoTransaccao.RegistadoPEnviar)) || (m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.GetItem(ref tempRefParam44)) == 1) || (m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.GetItem(ref tempRefParam45)) == 11);

						}

						objCampos = null;

						//Para os documentos certificados, a validação já foi efetuada
						if (!blnTTEEditavel && !FuncoesComuns100.FuncoesBS.Documentos.DocSujeitoCertificacao2(clsTabVenda.TipoDocumento, ConstantesPrimavera100.Modulos.Vendas, clsTabVenda.BensCirculacao, objSerie.AutoFacturacao, clsDocumentoVenda.DataDoc))
						{
							if (m_objErpBSO.Contexto.LocalizacaoSede != ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha)
							{
								//Devido ao SII não é necessário validar se houve alterações no documento. Vai ser sempre comunicado.


								strMensagemErro = StrErro; //BID 593536

								//Documentos integrados em TTE apenas podem ser re-gravados em condições limitadas
								//Devido ao SII vai não vai validar se o documento foi alterado.
								if (!CertificacaoSoftware.ValidaEdicao_DetalheDocumento(clsDocumentoVenda, ref StrErro, true))
								{

									//BID 593536 : Neste cenário, a mensagem de erro não pode ser "Foram efectuadas alterações no documento com impacto fiscal/legal"
									StrErro = strMensagemErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15144, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + "(" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14879, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ")" + Environment.NewLine;

									result = false;

								}

							}

						}

					}
					//Fim 587571/588035

				}

				//CS.3866
				if (clsDocumentoVenda.Resumo)
				{
					//BID 583138 (estava "clsSerie" em vez de "objSerie")
					if (objSerie.Origem != ((int) BasBETiposGcp.EnumSerieOrigem.IntegradoOutraAplicacao))
					{
						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16592, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
					}
				}

				//Valida a Data e Hora de carga
				//BID 583138 (estava "clsSerie" em vez de "objSerie")
				//BID 583276 (foi adicionado o parâmetro "strErro")
				if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaDataHoraCarga(objSerie, clsTabVenda, clsDocumentoVenda, ConstantesPrimavera100.Modulos.Vendas, ref StrErro))
				{

					result = false;
					//StrErro = StrErro & m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16606, FuncoesComuns100.ModuloGCP) & vbCrLf 'BID 583276

				}

				//CS.3405 - Adiantamentos em CC
				if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaGravacaoAdiantamentos(clsTabVenda, clsDocumentoVenda, objSerie.AutoFacturacao, ref StrErro))
				{

					result = false;

				}

				//CS.4214 - Planos de Prestações
				if (clsTabVenda.LigacaoCC)
				{

					if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.PlanosPagamentos.ExistePlanoPagamentosDoc(clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc)))
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17163, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}

				//        'Epic 406 - Valida o contrato
				if (Strings.Len(clsDocumentoVenda.IdContrato) > 0)
				{

					result = result && Contratos.ValidaContrato((dynamic) clsDocumentoVenda, ref StrErro, ref Avisos);

				}

				//BID 599717
				if (Strings.Len(clsDocumentoVenda.IDCabecMovCbl) > 0)
				{

					if (FuncoesComuns100.FuncoesBS.Documentos.VerificaApuramentoIva(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, clsDocumentoVenda.NumDoc, clsDocumentoVenda.Filial, clsDocumentoVenda.CBLAno))
					{

						result = false;
						//UPGRADE_WARNING: (6021) Casting 'ErpBS100.EnumLocalizacaoSede' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
						string tempRefParam46 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(7759, ConstantesPrimavera100.AbreviaturasApl.Contabilidade);
						StdBE100.StdBETipos.EnumConstantesGlobais tempRefParam47 = StdBE100.StdBETipos.EnumConstantesGlobais.cgIVA;
						StdBE100.StdBETipos.EnumLocalizacaoSede tempRefParam48 = (StdBE100.StdBETipos.EnumLocalizacaoSede) m_objErpBSO.Contexto.LocalizacaoSede;
						dynamic[] tempRefParam49 = new dynamic[]{m_objErpBSO.DSO.Plat.Localizacao.ConstanteLocalizada(tempRefParam47, tempRefParam48)};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam46, tempRefParam49) + Environment.NewLine;

					}

				}

				//US 15076
				if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha && clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro) && Strings.Len(clsTabVenda.SAFTTipoDoc) == 0)
				{

					result = false;
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16426, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}
				//^US 15076


				//** Efectua as validações necessárias para cada linha do documento de venda
				result = result && ValidaActualizacaoLinhas(clsDocumentoVenda, clsDocumentoVenda.Linhas, clsTabVenda, ref StrErro, ref blnDocTransfEstornado, ref Avisos);

				//BID 17369
				blnUltimoDocumentoVenda = (clsDocumentoVenda.NumDoc == m_objErpBSO.Base.Series.ProximoNumero(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie) - 1);

				//User Story 12310:Como Diretor financeiro, pretendo ter restrições na utilização do NIF de terceiros, para cumprir com os requisitos legais do despacho nº 8632/2014
				if ((!clsDocumentoVenda.EmModoEdicao || blnUltimoDocumentoVenda) && !blnDocTransfEstornado && FuncoesComuns100.FuncoesBS.Documentos.DocSujeitoCertificacao2(clsTabVenda.TipoDocumento, ConstantesPrimavera100.Modulos.Vendas, clsTabVenda.BensCirculacao, objSerie.AutoFacturacao, clsDocumentoVenda.DataDoc))
				{

					string tempRefParam50 = clsDocumentoVenda.TipoEntidadeFac;
					string tempRefParam51 = clsDocumentoVenda.EntidadeFac;
					if (!m_objErpBSO.Base.FuncoesGlobais.ValidaAlteracaoNifEntidades(ref tempRefParam50, ref tempRefParam51, clsDocumentoVenda.NumContribuinteFac, false))
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17684, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}

				//Se é uma encomenda e está em edição vamos verificar se está numa necessidade de compra e se é possível _
				//remover essa necessidade
				if (clsDocumentoVenda.EmModoEdicao && clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
				{

					//Se vamos fechar o documento, validamos se existe alguma linha em necessidades
					if (clsDocumentoVenda.Fechado)
					{

						if (~Convert.ToInt32(m_objErpBSO.Compras.PlaneamentoCompras.ProcessaAlteracaoNecessidade(clsDocumentoVenda.ID, "", StrErro)) != 0)
						{

							result = false;
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17684, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						}

					}
					else
					{

						strXMLLinhasValidarNecessidade = FuncoesComuns100.FuncoesBS.Documentos.DevolveXMLLinhasValidarNecessidades(FuncoesComuns100.FuncoesBS.Documentos.PreencheLinhasValidarNecessidade(clsDocumentoVenda.Linhas));

						if (~Convert.ToInt32(m_objErpBSO.Compras.PlaneamentoCompras.ProcessaAlteracaoNecessidade(clsDocumentoVenda.ID, strXMLLinhasValidarNecessidade, StrErro)) != 0)
						{

							result = false;

						}

					}

				}

				objSerie = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValidaActualizacao", excep.Message);
			}
			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : ValorTotalAdiantamentos
		// Description : CS.3405 - Adiantamentos em CC
		// Arguments   : DocumentoVenda -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private double ValorTotalAdiantamentos(VndBE100.VndBEDocumentoVenda DocumentoVenda)
		{

			double dblTotalAdi = 0;

			foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha in DocumentoVenda.Linhas)
			{

				if (Strings.Len(ObjLinha.DadosAdiantamento.IDHistorico) > 0)
				{

					dblTotalAdi = dblTotalAdi + ObjLinha.PrecoLiquido + ObjLinha.TotalIva;

				}

			}

			return TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblTotalAdi, 2);

		}

		//Devolve o valor total das prestações.
		private double ValorTotalPrestacoes(BasBEPrestacoes ClsPrestacoes)
		{
			double Valor = 0;

			try
			{

				foreach (BasBEPrestacao ClsPrestacao in ClsPrestacoes)
				{
					Valor = ClsPrestacao.Valor + Valor;
				} //BID 568425

				return TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(Valor, 2);
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValorTotalPrestacoes", excep.Message);
			}
			return 0;
		}

		//---------------------------------------------------------------------------------------
		// Procedure     : ValidaTransElectronica
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		private bool ValidaTransElectronica(VndBE100.VndBEDocumentoVenda DocVenda, ref string StrErro)
		{

			bool result = false;
			try
			{

				result = true;

				//Valida a transação eletrónica
				if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaTransacaoElectronica(DocVenda, ConstantesPrimavera100.Modulos.Vendas, ref StrErro))
				{

					result = false;

				}

				//Transações apenas permitidas se não existirem quaisquer linhas de custos adicionais ou diferenças de cálculo
				foreach (VndBE100.VndBELinhaDocumentoVenda objLinhaVenda in DocVenda.Linhas)
				{

					if ((String.CompareOrdinal(objLinhaVenda.TipoLinha, ConstantesPrimavera100.Documentos.TipoLinCustosAdicionais) >= 0) && (String.CompareOrdinal(objLinhaVenda.TipoLinha, "89") <= 0))
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(12092, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						break;

					}

				}
			}
			catch (System.Exception excep)
			{

				result = false;
				StrErro = StrErro + excep.Message + Environment.NewLine;
			}

			return result;
		}

		private double DaTotalPendenteReservas(ref string IDLinhaOriginal, bool StockPrevisto)
		{
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			double dblTotal = 0;

			try
			{

				strSQL = "SELECT Total = ISNULL(SUM(QuantidadePendente),0) " + Environment.NewLine + 
				         "FROM INV_Reservas R " + Environment.NewLine + 
				         "INNER JOIN INV_Estados E ON R.EstadoDestino = E.Estado " + Environment.NewLine + 
				         "WHERE R.IdChaveDestino = '@1@' AND R.IdTipoOrigemDestino = '@2@' AND E.Existencias = @3@ ";

				dynamic[] tempRefParam = new dynamic[]{IDLinhaOriginal, m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), (StockPrevisto) ? 0 : 1};
				strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

				objLista = m_objErpBSO.Consulta(strSQL);

				dblTotal = 0;

				if (!objLista.Vazia())
				{
					dblTotal = m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("Total"));
				}

				objLista = null;


				return dblTotal;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "DaTotalPendenteReservas", excep.Message);
			}
			return 0;
		}

		//Efectua as validações necessárias à actualização das linhas de um documento de venda.
		private bool ValidaActualizacaoLinhas(VndBE100.VndBEDocumentoVenda clsDocVenda, VndBE100.VndBELinhasDocumentoVenda clsLinhasVenda, VndBE100.VndBETabVenda clsTabVenda, ref string StrErro, ref bool DocTransfEstornado, ref string Avisos)
		{
			//##PARAM clsDocumentoVenda Objecto que define o documento de venda a validar.
			bool result = false;
			VndBE100.VndBELinhaDocumentoVenda Linha = null;
			BasBELinhaDocumentoDim LinhaDim = null;
			StdBE100.StdBECampos objCamposLinhaOrig = null;
			StdBE100.StdBECampos objCamposLinha = null;
			OrderedDictionary objCacheArm = null; //CS.682
			StdBE100.StdBELista objListaDocsEstorn = null;
			FuncoesComuns100.clsBSProjectos objProjecto = null;
			bool bMesmaObra = false;
			bool blnValidouIvaEcovalor = false;
			int lngIndice = 0;
			double dblQuantidade = 0;
			bool blnArtigoAutoCCOP = false; // CR.714
			double dblQuantSatisf = 0;
			double dblQuantTrans = 0;
			string strIdOrigem = "";
			string strSQL = "";
			OrderedDictionary objColIvaDescontos = null; //CS.3605 - SAFT Parte 2
			double dblValorIva = 0;
			BasBEResumoIva objResumoIVA = null;
			//CS.3483
			string strRegimeMargemIva = "";
			OrderedDictionary colTaxasIva = null;
			string strCodIva = "";
			//US.35450
			string strSQLQuantCopiada = "";
			string strSQLQuantTrans = "";
			StdBE100.StdBECampos objCamposArt = null;
			//BID 595793
			string strArtigo = "";
			string strErroLinha = "";
			//^BID 595793
			//BID 598089
			OrderedDictionary colLinhasDoc = null;
			double dblQuantPendRes = 0;
			string strTipoMovStock = "";
			string strIdOrg = "";
			double dblQtdUnBase = 0;
			double dblTotalResUnDoc = 0;

			try
			{

				//strIdLinhaOrig = vbNullString 'BID 573251/577161
				bMesmaObra = true;
				blnValidouIvaEcovalor = false;
				result = true;
				lngIndice = 1;

				DocTransfEstornado = false;

				objCacheArm = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase); //CS.682
				objProjecto = FuncoesComuns100.FuncoesBS.Instancia_Projectos;
				objProjecto.Modulo = ConstantesPrimavera100.Modulos.Vendas;

				//CS.3605 - SAFT Parte 2
				objColIvaDescontos = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
				colTaxasIva = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
				//BID 598089
				colLinhasDoc = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);


				foreach (VndBE100.VndBELinhaDocumentoVenda Linha2 in clsLinhasVenda)
				{
					Linha = Linha2;


					if (Linha.FactorConv != 1)
					{

						string tempRefParam = Linha.Artigo;
						string tempRefParam2 = Linha.Unidade;
						double tempRefParam3 = Linha.Quantidade;
						dblQtdUnBase = m_objErpBSO.Base.Artigos.ConverteQtdParaUnidadeBase(ref tempRefParam, ref tempRefParam2, ref tempRefParam3);

					}
					else
					{

						dblQtdUnBase = Linha.Quantidade;

					}
					//Forçar o lote por defeito na linha
					if (Strings.Len(Linha.Artigo) != 0 && Linha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo && Strings.Len(Linha.Lote) == 0)
					{

						Linha.Lote = ConstantesPrimavera100.Inventario.LotePorDefeito;

					}

					//BID 598089
					if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(Linha.IdLinha, colLinhasDoc) && Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(Linha.IdLinha)) > 0 && Linha.EstadoBD != BasBETiposGcp.enuEstadosBD.estNovo)
					{

						colLinhasDoc.Add(Linha.IdLinha, Linha.IdLinha);

					}
					//^BID 598089

					//BID 595793
					strArtigo = Linha.Artigo;

					//É um documento transformado...
					if ((Strings.Len(Linha.IdLinhaEstorno) > 0 || Strings.Len(Linha.IDLinhaOriginal) > 0) || (Strings.Len(Linha.IdLinhaOrigemCopia) > 0 && clsTabVenda.ControlaQtdSatisfeita))
					{

						DocTransfEstornado = true;

					}

					if (FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoLinha(Linha.TipoLinha))
					{

						//Limite de N taxas de IVA da U2LCalc
						strCodIva = Linha.CodIva.Trim().ToUpper();
						if (Strings.Len(strCodIva) > 0)
						{

							if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(strCodIva, colTaxasIva))
							{

								colTaxasIva.Add(strCodIva, strCodIva);

							}

						}


						FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(Linha.CamposUtil, DaDefCamposUtilLinhas());

						if (Information.IsNumeric(Linha.TipoLinha))
						{

							//BID: 575441
							if (Strings.Len(Linha.AlternativaGPR) > 0)
							{

								if (~Convert.ToInt32(m_objErpBSO.Producao.Alternativa.Existe(Linha.AlternativaGPR, Linha.Artigo)) != 0)
								{

									result = false;
									string tempRefParam4 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16198, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam5 = new dynamic[]{Linha.AlternativaGPR, Linha.Artigo};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam4, tempRefParam5) + Environment.NewLine;

								}

							}
							//^BID: 575441


							//BID 591035
							if (Strings.Len(Linha.CodIva) == 0 && Linha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinAcertos && Linha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario && Linha.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo)
							{

								result = false;
								string tempRefParam6 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3715, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam7 = new dynamic[]{lngIndice};
								StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14184, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + " (" + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam6, tempRefParam7) + ")" + Environment.NewLine;

							}
							//Fim 591035

							//CS.3605 - SAFT Parte 2
							if (StringsHelper.ToDoubleSafe(Linha.TipoLinha) == 40 || StringsHelper.ToDoubleSafe(Linha.TipoLinha) == 41 || StringsHelper.ToDoubleSafe(Linha.TipoLinha) == 90)
							{

								if (Strings.Len(Linha.CodIva) > 0)
								{

									//Se não existe, adiciona à collection
									if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(Linha.CodIva.ToUpper(), objColIvaDescontos))
									{

										objColIvaDescontos.Add(Linha.CodIva.ToUpper(), Linha.TotalIva);

									}
									else
									{

										//Se ja existe, incrementa
										dblValorIva = (double) objColIvaDescontos[Linha.CodIva.ToUpper()];
										dblValorIva += Linha.TotalIva;

										objColIvaDescontos.Remove(Linha.CodIva.ToUpper());
										objColIvaDescontos.Add(Linha.CodIva.ToUpper(), dblValorIva);

									}

								}

							}
							//END CS.3605

							//BID 599790 : não permitir gravar documentos com o código do Artigo Pai vazio
							if (Linha.TipoLinha == ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo)
							{

								if (!m_objErpBSO.Base.Artigos.Existe(Linha.Artigo))
								{

									result = false;
									string tempRefParam8 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3918, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam9 = new dynamic[]{Linha.Artigo};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam8, tempRefParam9) + Environment.NewLine;

								}

							}

							if ((StringsHelper.ToDoubleSafe(Linha.TipoLinha) >= 10 && StringsHelper.ToDoubleSafe(Linha.TipoLinha) <= 29) || StringsHelper.ToDoubleSafe(Linha.TipoLinha) == 91)
							{ //BID: 584523

								//CR.141 - Valida as linhas negativas
								if (Linha.Quantidade < 0 && !clsTabVenda.PermiteLinhasNegativas)
								{

									result = false;
									string tempRefParam10 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14497, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam11 = new dynamic[]{lngIndice};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam10, tempRefParam11) + Environment.NewLine;

								}

								//Na linha Starter Easy só é permitido artigos sujeitos a IEC no caso dos sacos leves
								objCamposArt = m_objErpBSO.Base.Artigos.DaValorAtributos(Linha.Artigo, "SujeitoIEC", "CategoriaIEC");

								//If Not m_objErpBSO.Base.Artigos.Existe(.Artigo) Then
								if (objCamposArt == null)
								{

									result = false;
									string tempRefParam12 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3918, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam13 = new dynamic[]{Linha.Artigo};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam12, tempRefParam13) + Environment.NewLine;

								}
								else
								{

									//Na linha Starter Easy só é permitido artigos sujeitos a IEC para os sacos leves
									if (m_objErpBSO.Base.Licenca.ApenasConfiguracaoStarterEasy)
									{

										string tempRefParam14 = "SujeitoIEC";
										if (m_objErpBSO.DSO.Plat.Utils.FBool(objCamposArt.GetItem(ref tempRefParam14)))
										{ //PriGlobal: IGNORE

											string tempRefParam16 = "CategoriaIEC";
											string tempRefParam15 = m_objErpBSO.DSO.Plat.Utils.FStr(objCamposArt.GetItem(ref tempRefParam16));
											string tempRefParam17 = "TipoIEC";
											if (m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Base.IECCategorias.DaValorAtributo(ref tempRefParam15, ref tempRefParam17)) != 1)
											{

												result = false;
												string tempRefParam18 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18201, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
												dynamic[] tempRefParam19 = new dynamic[]{Linha.Artigo, lngIndice};
												StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam18, tempRefParam19) + Environment.NewLine;

											}

										}

									}

									if ((Strings.Len(Linha.CodIva) == 0) && (Linha.LinhasHistoricoResiduo.NumItens > 0) && !blnValidouIvaEcovalor)
									{

										result = false;
										blnValidouIvaEcovalor = true;
										StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(12737, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

									}

									// CR.714 Alfa7 - Se for o Artigo Auto definido nos Parametros_COP não valida a Unidade
									string tempRefParam20 = Linha.Artigo;
									string tempRefParam21 = clsDocVenda.Tipodoc;
									blnArtigoAutoCCOP = ArtigoAutoCCOP(Linha.AutoID, Linha.IDObra, ref tempRefParam20, ref tempRefParam21);

									if (!blnArtigoAutoCCOP)
									{

										// Valida a Unidade
										if (Strings.Len(Linha.Unidade) == 0)
										{

											result = false;
											string tempRefParam22 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9084, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											dynamic[] tempRefParam23 = new dynamic[]{Linha.Artigo, lngIndice};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam22, tempRefParam23) + Environment.NewLine;

										}

									}

									if (Linha.NumerosSerie.NumItens > Math.Abs(TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(Linha.Quantidade * Linha.FactorConv, 0)))
									{

										for (int lngIndiceNSerie = Linha.NumerosSerie.NumItens; lngIndiceNSerie >= Math.Abs(TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(Linha.Quantidade * Linha.FactorConv, 0)) + 1; lngIndiceNSerie--)
										{

											Linha.NumerosSerie.Remove(lngIndiceNSerie);

										}

									}


									if (clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda))
									{

										//UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
										if (Linha.DataEntrega == DateTime.FromOADate(0) || Linha.DataEntrega.Equals(DateTime.FromOADate(0)))
										{

											result = false;
											string tempRefParam24 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9088, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											dynamic[] tempRefParam25 = new dynamic[]{Linha.Artigo, lngIndice};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam24, tempRefParam25) + Environment.NewLine;

										}

										if (Linha.ReservaStock != null)
										{

											if (Linha.ReservaStock.Linhas.NumItens > 0)
											{

												dblTotalResUnDoc = DaTotalReservado(Linha.ReservaStock.Linhas);

												if (dblTotalResUnDoc > dblQtdUnBase)
												{

													result = false;
													StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(790, ConstantesPrimavera100.AbreviaturasApl.Base) + Environment.NewLine;


												}

												if (Linha.FactorConv != 1)
												{

													string tempRefParam26 = Linha.Artigo;
													string tempRefParam27 = Linha.Unidade;
													double tempRefParam28 = Linha.Quantidade;
													dblTotalResUnDoc = m_objErpBSO.Base.Artigos.ConverteQtdParaUnidade(ref tempRefParam26, ref tempRefParam27, ref tempRefParam28);

												}

												Linha.QuantReservada = dblTotalResUnDoc;
												//Verificar se o artigo trata números de série e se os tem preenchidos, se não tem, vai lêr aos documentos origem da reserva
												if (m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Artigos.DaValorAtributo(Linha.Artigo, "TratamentoSeries")))
												{

													PreencheNumerosSerieDocsReserva(Linha);

												}

											}

										}

									}

									result = FuncoesComuns100.FuncoesBS.Documentos.ValidaNumerosSerieTransformacao(Linha, ref StrErro);


									if (Linha.MovStock == "S" && clsTabVenda.LigacaoStocks)
									{

										//Se o estado origem e destino são iguais não existe movimentação de stock, logo, não serão criadas novas reservas
										if (Linha.INV_EstadoOrigem.Trim().ToUpper() == Linha.INV_EstadoDestino.Trim().ToUpper())
										{

											//Linha.ReservaStock = new dynamic();
											Linha.INV_IDReserva = "";

										}
										//Validações números de série
										if (Linha.Quantidade >= 0)
										{

											strTipoMovStock = clsTabVenda.TipoMovStock;

										}
										else
										{

											strTipoMovStock = clsTabVenda.NTipoMovStock;

										}

										//Faz a validação/Preenchimento dos números de série apenas para as entradas
										if (strTipoMovStock == ConstantesPrimavera100.Inventario.MovimentoEntrada)
										{

											FuncoesComuns100.FuncoesBS.Documentos.ValidaNumerosSerieLinha(Linha);

										}
										//Valida os estados do inventário na linha
										result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaEstadosInventarioLinha(lngIndice, clsDocVenda.Tipodoc, Linha, ref StrErro);

										// Valida a data de Saida
										if (!Information.IsDate(Linha.DataStock))
										{

											result = false;
											string tempRefParam29 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9089, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											dynamic[] tempRefParam30 = new dynamic[]{Linha.DataStock, Linha.Artigo};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam29, tempRefParam30) + Environment.NewLine;

										}
										else if (Linha.DataStock < DateTime.Parse("01-01-1900"))
										{ 

											result = false;
											string tempRefParam31 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9090, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											dynamic[] tempRefParam32 = new dynamic[]{Linha.DataStock, Linha.Artigo};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam31, tempRefParam32) + Environment.NewLine;

										}

										if (Strings.Len(Linha.IDLinhaOriginal) > 0)
										{

											strIdOrg = Linha.IDLinhaOriginal;

										}
										else
										{

											if (ValidaCondicoesCopiaReserva(Linha, clsTabVenda))
											{

												strIdOrg = Linha.IdLinhaOrigemCopia;

											}

										}

										if (Strings.Len(strIdOrg) > 0)
										{

											dblQuantPendRes = DaTotalPendenteReservas(ref strIdOrg, false);

											//Reservas disponiveis são suficientes ?
											if (Math.Abs(dblQuantPendRes) < Math.Abs(dblQtdUnBase))
											{

												//Existem reservas de stock previsto ?
												dblQuantPendRes = DaTotalPendenteReservas(ref strIdOrg, true);

												//Se existirem reservas de stock previsto então devem ser efectivadas 1º
												if (dblQuantPendRes > 0)
												{

													result = false;
													StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(18306, FuncoesComuns100.InterfaceComunsUS.ModuloActual) + Environment.NewLine;

												}
											}

										}

										//BID 547240
										if (Strings.Len(Linha.Armazem) > 0)
										{

											//CS.682
											// Verifica se o armazém pode ser movimentado (não está bloqueado)
											if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(Linha.Armazem, objCacheArm))
											{

												objCacheArm.Add(Linha.Armazem, m_objErpBSO.Inventario.Armazens.Edita(Linha.Armazem));

											}

											if (strTipoMovStock == ConstantesPrimavera100.Inventario.MovimentoEntrada)
											{

												// Movimento de entrada de stock
												//if (Convert.ToBoolean(objCacheArm[Linha.Armazem].BloqueadoEntradas))
												//{

												//	result = false;
												//	string tempRefParam33 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15336, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
												//	dynamic[] tempRefParam34 = new dynamic[]{Linha.Armazem};
												//	StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam33, tempRefParam34) + Environment.NewLine; //CS.682 | TODO: Localizar mensagem!

												//}

											}
											else
											{

												// Movimento de saída de stock
												//if (Convert.ToBoolean(objCacheArm[Linha.Armazem].BloqueadoSaidas))
												//{

												//	result = false;
												//	string tempRefParam35 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15335, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
												//	dynamic[] tempRefParam36 = new dynamic[]{Linha.Armazem};
												//	StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam35, tempRefParam36) + Environment.NewLine; //CS.682 | TODO: Localizar mensagem!

												//}

											}

										}

									}
									else
									{

										//Se não liga a stocks não tem estados (excepto reserevas)
										if (!clsTabVenda.ReservaAutomatica)
										{

											Linha.INV_EstadoOrigem = "";
											Linha.INV_EstadoDestino = "";

										}

									}

									// CR.714 Alfa7 - Se for o Artigo Auto definido nos Parametros_COP não valida nem Armazem nem Localizacao
									if (!blnArtigoAutoCCOP)
									{

										if (Linha.MovStock == "S" && Strings.Len(Linha.Armazem) == 0 && clsTabVenda.LigacaoStocks)
										{

											result = false;
											string tempRefParam37 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9792, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											dynamic[] tempRefParam38 = new dynamic[]{lngIndice};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam37, tempRefParam38) + Environment.NewLine;

										}

										if (Linha.MovStock == "S" && clsTabVenda.LigacaoStocks)
										{

											if (Strings.Len(Linha.Localizacao) == 0)
											{

												result = false;
												string tempRefParam39 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9793, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
												dynamic[] tempRefParam40 = new dynamic[]{lngIndice};
												StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam39, tempRefParam40) + Environment.NewLine;

											}

										}

									}
									//^CR.714


									dblQuantidade = 0;

									if (clsDocVenda.EmModoEdicao)
									{
										// Informação relativa ao documento/linha actual
										string tempRefParam41 = Linha.IdLinha;
										dynamic[] tempRefParam42 = new dynamic[]{"Quantidade"};
										objCamposLinha = m_objErpBSO.Vendas.Documentos.DaValorAtributosIDLinha(tempRefParam41, tempRefParam42);

										if (objCamposLinha != null)
										{

											string tempRefParam43 = "Quantidade";
											dblQuantidade = Math.Abs(ReflectionHelper.GetPrimitiveValue<double>(objCamposLinha.GetItem(ref tempRefParam43).Valor)); //PriGlobal: IGNORE

										}
									}

									//BID 524886
									//BID 578500 (foi adicionado o teste "And Len(.IdLinhaEstorno) = 0")
									if (Strings.Len(Linha.IDLinhaOriginal) > 0 && clsTabVenda.TipoDocumento > ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda) && Strings.Len(Linha.IdLinhaEstorno) == 0)
									{

										// Informação relativa ao documento/linha original
										string tempRefParam44 = Linha.IDLinhaOriginal;
										objCamposLinhaOrig = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosLinhaOrig(ref tempRefParam44);
										strIdOrigem = Linha.IDLinhaOriginal;

										strSQLQuantCopiada = FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.TrataCampoQtdCopiada("DOC", "LS"); //PriGlobal: IGNORE
										strSQLQuantTrans = "LS.QuantTrans"; //PriGlobal: IGNORE

										//.ModuloOrigemCopia = ConstantesPrimavera100.MODULOS.VENDAS --> No caso de geração de documentos de venda que acompanham os documentos de Stock, o modulo origem é Stk
									}
									else if (clsTabVenda.ControlaQtdSatisfeita && Strings.Len(Linha.IdLinhaOrigemCopia) > 0 && (Linha.ModuloOrigemCopia == ConstantesPrimavera100.Modulos.Vendas))
									{ 

										// Informação relativa ao documento/linha original
										string tempRefParam45 = Linha.IdLinhaOrigemCopia;
										objCamposLinhaOrig = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosLinhaOrig(ref tempRefParam45);
										strIdOrigem = Linha.IdLinhaOrigemCopia;

										strSQLQuantCopiada = "LS.QuantCopiada"; //PriGlobal: IGNORE
										strSQLQuantTrans = FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.TrataCampoQtdTransformada("DOC", "LS"); //PriGlobal: IGNORE

									}

									//CS.3085
									//BID 579525 (descomentou-se a 1ª linha e comentou-se a 2ªlinha)
									if (objCamposLinhaOrig != null)
									{

										//UPGRADE_WARNING: (6021) Casting 'Variant' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
										string tempRefParam46 = "TipoDocumento";
										string tempRefParam47 = "LigaStocks";
										if ((((((BasBETipos.LOGTipoDocumento) ReflectionHelper.GetPrimitiveValue<int>(objCamposLinhaOrig.GetItem(ref tempRefParam46).Valor)) >= BasBETipos.LOGTipoDocumento.LOGDocStk_Transporte) ? -1 : 0) & ReflectionHelper.GetPrimitiveValue<int>(objCamposLinhaOrig.GetItem(ref tempRefParam47).Valor)) != 0)
										{ //PriGlobal: IGNORE
											//Fim 579525

											//Quantidade já transformada
											//BID 581479
											strSQL = "SELECT QuantTrans = SUM(QuantTrans) FROM (" + "\r";
											strSQL = strSQL + "SELECT QuantTrans = ISNULL(SUM(@1@+@2@),0)" + "\r";
											strSQL = strSQL + "FROM LinhasDocStatus LS " + "\r";
											strSQL = strSQL + "INNER JOIN LinhasDoc LD ON LD.Id = LS.IdLinhasDoc " + "\r";
											strSQL = strSQL + "INNER JOIN CabecDoc CD ON CD.Id = LD.IdCabecDoc " + "\r";
											strSQL = strSQL + "INNER JOIN DocumentosVenda DOC ON DOC.Documento = CD.TipoDoc " + "\r";
											strSQL = strSQL + "WHERE LS.IdLinhasDoc='" + strIdOrigem + "'" + "\r";
											strSQL = strSQL + ") TEMP " + "\r";
											//Fim 581479

											dynamic[] tempRefParam48 = new dynamic[]{strSQLQuantTrans, strSQLQuantCopiada};
											strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam48);
											objListaDocsEstorn = m_objErpBSO.Consulta(strSQL);

											if (!objListaDocsEstorn.Vazia())
											{

												dblQuantSatisf = m_objErpBSO.DSO.Plat.Utils.FDbl(objListaDocsEstorn.Valor("QuantTrans")); //PriGlobal: IGNORE

											}
											else
											{

												dblQuantSatisf = 0;

											}

											//Quantidade em rascunho
											//BID 581479
											//strSQL = "SELECT QuantTrans = SUM(QuantTrans) FROM (" & vbCr
											//strSQL = strSQL & "SELECT QuantTrans = ISNULL(SUM(ISNULL(QuantTrans,0)),0)" & vbCr
											//strSQL = strSQL & "FROM LinhasDocTransRascunhos " & vbCr
											//strSQL = strSQL & "WHERE IdLinhasDocOrigem='" & strIdOrigem & "'" & vbCr
											//strSQL = strSQL & "UNION ALL" & vbCr
											//strSQL = strSQL & "SELECT QuantTrans = ISNULL(SUM(ISNULL(Quantidade,0)),0)" & vbCr
											//strSQL = strSQL & "FROM LinhasDocRascunhos " & vbCr
											//strSQL = strSQL & "WHERE IdLinhaOrigemCopia='" & strIdOrigem & "'" & vbCr
											//strSQL = strSQL & ") TEMP " & vbCr
											strSQL = "SELECT QuantTrans = SUM(QuantTrans) FROM (" + "\r";
											strSQL = strSQL + "SELECT QuantTrans = ISNULL(SUM(@1@+@2@),0)" + "\r";
											strSQL = strSQL + "FROM LinhasDocStatusRascunhos LS " + "\r";
											strSQL = strSQL + "INNER JOIN LinhasDocRascunhos LD ON LD.Id = LS.IdLinhasDoc " + "\r";
											strSQL = strSQL + "INNER JOIN CabecDocRascunhos CD ON CD.Id = LD.IdCabecDoc " + "\r";
											strSQL = strSQL + "INNER JOIN DocumentosVenda DOC ON DOC.Documento = CD.TipoDoc " + "\r";
											strSQL = strSQL + "WHERE LS.IdLinhasDoc='" + strIdOrigem + "'" + "\r";
											strSQL = strSQL + ") TEMP " + "\r";
											//Fim 581479

											dynamic[] tempRefParam49 = new dynamic[]{strSQLQuantTrans, strSQLQuantCopiada};
											strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam49);

											objListaDocsEstorn = m_objErpBSO.Consulta(strSQL);

											if (!objListaDocsEstorn.Vazia())
											{

												dblQuantTrans = m_objErpBSO.DSO.Plat.Utils.FDbl(objListaDocsEstorn.Valor("QuantTrans")); //PriGlobal: IGNORE

											}
											else
											{

												dblQuantTrans = 0;

											}

											//Se documento é um racunho
											string tempRefParam50 = clsDocVenda.ID;
											if (m_objErpBSO.Vendas.Documentos.ExisteRascunhoID(tempRefParam50))
											{

												strSQL = "SELECT Quantidade = SUM(ISNULL(Quantidade,0))" + "\r";
												strSQL = strSQL + "FROM LinhasDocRascunhos " + "\r";
												strSQL = strSQL + "WHERE Id='" + Linha.IdLinha + "'" + "\r";

												objListaDocsEstorn = m_objErpBSO.Consulta(strSQL);

												//BID 584890 (foi adicionada a utilização da função "DaFactorNaturezaDoc")
												dblQuantTrans -= (Double.Parse(objListaDocsEstorn.Valor("Quantidade")) * FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc)); //PriGlobal: IGNORE

												//BID 592050
											}
											else if (Strings.Len(clsDocVenda.IDEstorno) > 0 && Linha.MovStock == "S")
											{ 

												strSQL = "SELECT cd.TipoDoc, Quantidade = ISNULL(ld.Quantidade,0) ";
												strSQL = strSQL + "FROM CabecDoc cd ";
												strSQL = strSQL + "INNER JOIN LinhasDoc ld ON ld.IdCabecDoc=cd.Id ";
												strSQL = strSQL + "INNER JOIN LinhasDocTrans ldt ON ldt.IdLinhasDoc=ld.IDLinhaEstorno ";
												strSQL = strSQL + "WHERE cd.Id='" + clsDocVenda.IDEstorno + "' AND ldt.IdLinhasDocOrigem='" + Linha.IDLinhaOriginal + "'";
												objListaDocsEstorn = m_objErpBSO.Consulta(strSQL);

												dblQuantSatisf -= (Double.Parse(objListaDocsEstorn.Valor("Quantidade")) * FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, objListaDocsEstorn.Valor("TipoDoc"))); //PriGlobal: IGNORE
												//Fim 592050

											}

											if (clsTabVenda.ControlaQtdSatisfeita || Strings.Len(Linha.IDLinhaOriginal) != 0)
											{ //BID (9.05): 597228
												//Compara qtd transformada e reservada com a original
												//BID 584428 (a variável "dblQuantidade" estava erradamente dentro do "ABS(...)")
												string tempRefParam51 = "Quantidade";
												if (Math.Abs(dblQuantSatisf + dblQuantTrans + Linha.Quantidade) - dblQuantidade > Math.Abs(ReflectionHelper.GetPrimitiveValue<double>(objCamposLinhaOrig.GetItem(ref tempRefParam51).Valor)))
												{ //PriGlobal: IGNORE

													//Quantidade disponível
													dblQuantTrans = Math.Abs(dblQuantidade) - Math.Abs(dblQuantSatisf + dblQuantTrans);

													switch(clsTabVenda.OperacaoControlaQtdSatisfeita)
													{
														case 0 :  //Aviso 
															string tempRefParam52 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9799, FuncoesComuns100.InterfaceComunsUS.ModuloGCP); 
															dynamic[] tempRefParam53 = new dynamic[]{Linha.Quantidade, lngIndice, dblQuantTrans}; 
															Avisos = Avisos + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam52, tempRefParam53) + Environment.NewLine;  //PriGlobal: IGNORE 
															break;
														case 1 :  //Erro 
															result = false; 
															string tempRefParam54 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9799, FuncoesComuns100.InterfaceComunsUS.ModuloGCP); 
															dynamic[] tempRefParam55 = new dynamic[]{Linha.Quantidade, lngIndice, dblQuantTrans}; 
															StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam54, tempRefParam55) + Environment.NewLine;  //PriGlobal: IGNORE 
															break;
														case 2 :  //Ignora 
															//NOP 
															break;
													}

												}
											}

										}

									}

									//CS.4013 - Melhorias ao processo de comunicação de faturas
									//BID 584075 (foi adicionado o teste "And Not clsDocVenda.EmModoEdicao")
									if ((!clsDocVenda.EmModoEdicao) && (!ValidaActualizacaoLinhaOrigTransf(objCamposLinhaOrig)))
									{

										result = false;
										string tempRefParam56 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16645, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
										string tempRefParam57 = "TipoDoc";
										string tempRefParam58 = "Numdoc";
										string tempRefParam59 = "Serie";
										dynamic[] tempRefParam60 = new dynamic[]{"" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam57).Valor), m_objErpBSO.DSO.Plat.Utils.FLng(objCamposLinhaOrig.GetItem(ref tempRefParam58)), "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam59).Valor)};
										StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam56, tempRefParam60) + Environment.NewLine; //PriGlobal: IGNORE

									}

									objCamposLinha = null;
									objCamposLinhaOrig = null;

									//CS.3483

									BasBETiposGcp.EnumRegimesIncidenciaIva switchVar = Linha.RegraCalculoIncidencia;
									if (switchVar == BasBETiposGcp.EnumRegimesIncidenciaIva.MargemCustoPadrao || switchVar == BasBETiposGcp.EnumRegimesIncidenciaIva.MargemPCM)
									{

										if (Linha.PercIvaDedutivel != 100)
										{

											if (Linha.RegraCalculoIncidencia == BasBETiposGcp.EnumRegimesIncidenciaIva.MargemPCM)
											{
												strRegimeMargemIva = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16407, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											}
											else
											{
												strRegimeMargemIva = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16408, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											}

											result = false;
											//UPGRADE_WARNING: (6021) Casting 'ErpBS100.EnumLocalizacaoSede' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
											string tempRefParam61 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16419, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
											StdBE100.StdBETipos.EnumConstantesGlobais tempRefParam62 = StdBE100.StdBETipos.EnumConstantesGlobais.cgIVA;
											StdBE100.StdBETipos.EnumLocalizacaoSede tempRefParam63 = (StdBE100.StdBETipos.EnumLocalizacaoSede) m_objErpBSO.Contexto.LocalizacaoSede;
											dynamic[] tempRefParam64 = new dynamic[]{Linha.Artigo, lngIndice, m_objErpBSO.DSO.Plat.Localizacao.ConstanteLocalizada(tempRefParam62, tempRefParam63), strRegimeMargemIva};
											StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam61, tempRefParam64);

										}

									}
									else
									{
									}
									//^CS.3483

								}

							}

							//Valida se o adiamtamento utilizado está definido para a entidade do documento.
							if (StringsHelper.ToDoubleSafe(Linha.TipoLinha) == 90)
							{

								if (Strings.Len(Linha.IDHistorico) > 0)
								{

									if (Convert.ToString(m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributoID(Linha.IDHistorico, "Entidade")) != clsDocVenda.EntidadeFac)
									{

										result = false;
										string tempRefParam65 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13276, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
										dynamic[] tempRefParam66 = new dynamic[]{lngIndice, clsDocVenda.Entidade};
										StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam65, tempRefParam66) + Environment.NewLine;

									}

								}

							}

							if (Strings.Len(Linha.Armazem) != 0 && Strings.Len(Linha.Localizacao) != 0)
							{

								if (~Convert.ToInt32(m_objErpBSO.Inventario.ArmazemLocalizacao.ExisteLocArmazem(Linha.Localizacao, Linha.Armazem)) != 0)
								{

									result = false;
									string tempRefParam67 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14110, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam68 = new dynamic[]{Linha.Localizacao, Linha.Armazem};
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam67, tempRefParam68) + Environment.NewLine;

								}

							}

							//CS.2489
							//CS.1398 - Não pode ter Tipo de Operacção nas linhas, se não for intracomunitário
							//If LenB(.TipoOperacao) > 0 And clsDocVenda.RegimeIva <> MercadoIntracomunitario Then
							//    ValidaActualizacaoLinhas = False
							//    StrErro = StrErro & m_objErpBSO.DSO.Plat.Strings.Formata(m_objErpBSO.DSO.Plat.Localizacao.DaResStringapl(15626, FuncoesComuns100.ModuloGCP), lngIndice, m_objErpBSO.DSO.Plat.Localizacao.ConstanteLocalizada(cgIVA, m_objErpBSO.Contexto.LocalizacaoSede)) & vbCrLf
							//End If
							if (Strings.Len(Linha.TipoOperacao) > 0)
							{
								string tempRefParam69 = Linha.TipoOperacao;
								result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoOperacao(ref StrErro, ConstantesPrimavera100.Modulos.Vendas, ref tempRefParam69, clsDocVenda.EspacoFiscal);
							}
							//Fim CS.2489

							//CS.3879 - só pode existir IVA ou Selo com valor -> Impostos não são cumulativos
							if (Linha.DadosImpostoSelo.ValorIS != 0 && Linha.TaxaIva != 0)
							{
								result = false;
								string tempRefParam70 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16629, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam71 = new dynamic[]{lngIndice};
								StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam70, tempRefParam71) + Environment.NewLine;
							}

							//                    'Epic 406 - Valida o contrato
							if (Strings.Len(Linha.IdContrato) > 0)
							{

								result = result && Contratos.ValidaContrato((dynamic) clsDocVenda, ref StrErro, ref Avisos, ref lngIndice);

							}

							result = result && objProjecto.ValidaLinhaDocumento(Linha, clsDocVenda.DataDoc, false, false, !clsTabVenda.LigacaoCC, ref lngIndice);
							StrErro = StrErro + objProjecto.ErroValidacao;

						}

					}
					else
					{

						result = false;
						string tempRefParam72 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9800, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam73 = new dynamic[]{lngIndice};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam72, tempRefParam73) + Environment.NewLine;

					}


					lngIndice++;

					Linha = null;
				}


				//CS.3605 - SAFT Parte 2
				if (objColIvaDescontos.Count > 0)
				{

					foreach (BasBEResumoIva objResumoIVA2 in clsDocVenda.ResumoIva)
					{
						objResumoIVA = objResumoIVA2;

						if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objResumoIVA.CodIva.ToUpper(), objColIvaDescontos))
						{

							dblValorIva = (double) objColIvaDescontos[objResumoIVA.CodIva.ToUpper()];

							if (Math.Abs(objResumoIVA.Valor + dblValorIva) < Math.Abs(dblValorIva) && dblValorIva > 0)
							{

								result = false;
								//UPGRADE_WARNING: (6021) Casting 'ErpBS100.EnumLocalizacaoSede' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
								string tempRefParam74 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16520, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								StdBE100.StdBETipos.EnumConstantesGlobais tempRefParam75 = StdBE100.StdBETipos.EnumConstantesGlobais.cgIVA;
								StdBE100.StdBETipos.EnumLocalizacaoSede tempRefParam76 = (StdBE100.StdBETipos.EnumLocalizacaoSede) m_objErpBSO.Contexto.LocalizacaoSede;
								dynamic[] tempRefParam77 = new dynamic[]{objResumoIVA.CodIva, m_objErpBSO.DSO.Plat.Localizacao.ConstanteLocalizada(tempRefParam75, tempRefParam76)};
								StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam74, tempRefParam77) + Environment.NewLine;

							}

						}

						objResumoIVA = null;
					}


				}

				//Limite de N taxas de IVA da U2LCalc
				if (colTaxasIva.Count > LogPRIAPIs.NUM_TAXAS_IVA)
				{

					result = false;
					//UPGRADE_WARNING: (6021) Casting 'ErpBS100.EnumLocalizacaoSede' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
					string tempRefParam78 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17629, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					StdBE100.StdBETipos.EnumConstantesGlobais tempRefParam79 = StdBE100.StdBETipos.EnumConstantesGlobais.cgIVA;
					StdBE100.StdBETipos.EnumLocalizacaoSede tempRefParam80 = (StdBE100.StdBETipos.EnumLocalizacaoSede) m_objErpBSO.Contexto.LocalizacaoSede;
					dynamic[] tempRefParam81 = new dynamic[]{LogPRIAPIs.NUM_TAXAS_IVA, m_objErpBSO.DSO.Plat.Localizacao.ConstanteLocalizada(tempRefParam79, tempRefParam80)};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam78, tempRefParam81) + Environment.NewLine;

				}

				//BID 573251/577161
				//If LenB(strIdLinhaOrig) > 0 Then
				//
				//    Set objDataDocOrig = m_objErpBSO.Consulta("SELECT TOP 1 cab.Data FROM CabecDoc cab WITH (NOLOCK) INNER JOIN LinhasDoc lin WITH (NOLOCK) ON lin.IdCabecDoc=cab.Id WHERE lin.Id IN (" & strIdLinhaOrig & ")  ORDER BY cab.Data DESC")
				//
				//    If Not objDataDocOrig.Vazia Then
				//
				//        If objDataDocOrig("Data") > clsDocVenda.DataDoc Then
				//
				//            ValidaActualizacaoLinhas = False
				//            StrErro = StrErro & m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16138, FuncoesComuns100.ModuloGCP) & vbCrLf
				//
				//        End If
				//
				//    End If
				//
				//    Set objDataDocOrig = Nothing
				//
				//End If
				//Fim 573251/577161

				if (clsDocVenda.EmModoEdicao)
				{

					//BID 571222/572539
					if (clsDocVenda.Linhas.Removidas != null)
					{

						//BID 571222
						if (clsDocVenda.Linhas.Removidas.NumItens > 0)
						{

							int tempForVar = clsDocVenda.Linhas.Removidas.NumItens;
							for (lngIndice = 1; lngIndice <= tempForVar; lngIndice++)
							{

								string tempRefParam82 = clsDocVenda.Linhas.Removidas.GetEdita(lngIndice);
								if (Math.Abs(m_objErpBSO.DSO.Plat.Utils.FDbl(m_objErpBSO.DSO.Vendas.Documentos.DaQntTransformada(ref tempRefParam82))) > 0)
								{

									result = false;
									StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16051, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
									break;

								}

								//BID 598089
								if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(clsDocVenda.Linhas.Removidas.GetEdita(lngIndice), colLinhasDoc))
								{

									clsDocVenda.Linhas.Removidas.Remove(clsDocVenda.Linhas.Removidas.GetEdita(lngIndice));

								}
								//^BID 598089

							}

						}

					}
					//Fim 571222/572539

				}
				else
				{

					clsDocVenda.Linhas.Removidas.RemoveTodos();

				}

				//Preencher o ID da obra no cabeçalho sempre que nas linhas seja o mesmo
				objProjecto.PreencheDocumento(clsDocVenda);

				Linha = null;
				LinhaDim = null;
				objCamposLinhaOrig = null;
				objCamposLinha = null;
				objCacheArm = null;
				objListaDocsEstorn = null;
				objProjecto = null;
				//CS.3605 - SAFT Parte 2
				objColIvaDescontos = null;
				objResumoIVA = null;
				colTaxasIva = null;
			}
			catch (System.Exception excep)
			{

				//CS.3605 - SAFT Parte 2

				//BID 595793
				string tempRefParam83 = "@1@ @2@ - @3@ - @4@";
				dynamic[] tempRefParam84 = new dynamic[]{m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(2791, FuncoesComuns100.InterfaceComunsUS.ModuloGCP), lngIndice, strArtigo, excep.Message};
				strErroLinha = m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam83, tempRefParam84);

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValidaActualizacaoLinhas", strErroLinha);
				//^BID 595793
			}

			return result;
		}

		private bool ValidaActualizacaoLinhas(VndBE100.VndBEDocumentoVenda clsDocVenda, VndBE100.VndBELinhasDocumentoVenda clsLinhasVenda, VndBE100.VndBETabVenda clsTabVenda, ref string StrErro, ref bool DocTransfEstornado)
		{
			string tempRefParam140 = "";
			return ValidaActualizacaoLinhas(clsDocVenda, clsLinhasVenda, clsTabVenda, ref StrErro, ref DocTransfEstornado, ref tempRefParam140);
		}

		private bool ValidaCondicoesCopiaReserva(VndBE100.VndBELinhaDocumentoVenda LinhaDoc, VndBE100.VndBETabVenda TabVenda)
		{
			bool result = false;
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			string strTipoMovimentoOrg = "";
			bool blnSeparaControloOrg = false;
			int intTipoDocumentoOrg = 0;

			try
			{

				result = false;

				if (Strings.Len(LinhaDoc.IdLinhaOrigemCopia) > 0)
				{

					strSQL = "SELECT D.SeparaControloQtdSatisfeita, D.TipoDocSTK, D.TipoDocumento FROM LinhasDoc (NOLOCK) L" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN CabecDoc (NOLOCK) C ON C.Id = L.IdCabecDoc" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN DocumentosVenda (NOLOCK) D ON D.Documento = C.TipoDoc" + Environment.NewLine;
					strSQL = strSQL + "WHERE L.Id = '@1@'";
					dynamic[] tempRefParam = new dynamic[]{LinhaDoc.IdLinhaOrigemCopia};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

					objLista = m_objErpBSO.Consulta(strSQL);
					dynamic tempRefParam2 = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam2))
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;

						strTipoMovimentoOrg = m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("TipoDocSTK"));
						blnSeparaControloOrg = m_objErpBSO.DSO.Plat.Utils.FBool(objLista.Valor("SeparaControloQtdSatisfeita"));
						intTipoDocumentoOrg = m_objErpBSO.DSO.Plat.Utils.FInt(objLista.Valor("TipoDocumento"));

						if (TabVenda.TipoDocumento > intTipoDocumentoOrg)
						{

							//Apenas se têm a mesma natureza
							if (TabVenda.TipoMovStock == strTipoMovimentoOrg)
							{

								if (!blnSeparaControloOrg && TabVenda.ControlaQtdSatisfeita)
								{

									result = true;

								}

							}

						}

						objLista = null;

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;
					}

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_ValidaCondicoesCopiaReserva", excep.Message);
			}

			return result;
		}

		private void PreencheNumerosSerieDocsReserva(VndBE100.VndBELinhaDocumentoVenda Linha)
		{
			string strIdsOrigem = "";
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			BasBENumeroSerie objNumSerie = null;
			string strEstadoRes = "";
			double dblQuantidadeDisp = 0;
			double dblQuantidadeRes = 0;
			OrderedDictionary colNSeriePreenchidos = null;
			int lngNumNSerieAntes = 0;
			OrderedDictionary colNSerieOrg = null;
			int lngDifNumSerie = 0;

			try
			{

				foreach (dynamic objLinhaReserva in Linha.ReservaStock)
				{

					if (Strings.Len(objLinhaReserva.IdChaveOrigem) > 0)
					{

						dblQuantidadeRes += objLinhaReserva.Quantidade;
						strIdsOrigem = strIdsOrigem + "'" + objLinhaReserva.IdChaveOrigem + "',";

					}
					else
					{

						//Reservas de DISP
						dblQuantidadeDisp += objLinhaReserva.Quantidade;
						if (Strings.Len(objLinhaReserva.EstadoOrigem) > 0)
						{

							strEstadoRes = strEstadoRes + "'" + objLinhaReserva.EstadoOrigem + "',";

						}

					}

				}

				if (Strings.Len(strIdsOrigem) == 0 && Strings.Len(strEstadoRes) == 0)
				{

					return;

				}

				//Alimentar uma coleção com os números de série definidos
				colNSeriePreenchidos = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
				colNSerieOrg = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
				//Para verificar se após a inserção dos números de série da reserva, temos os mesmo número de números de série
				lngNumNSerieAntes = Linha.NumerosSerie.NumItens;



				int tempForVar = Linha.NumerosSerie.NumItens;
				for (int lngIndice = 1; lngIndice <= tempForVar; lngIndice++)
				{

					if (Strings.Len(Linha.NumerosSerie.GetEdita(lngIndice).NumeroSerie) > 0)
					{

						colNSeriePreenchidos.Add(Linha.NumerosSerie.GetEdita(lngIndice).IdNumeroSerie, Linha.NumerosSerie.GetEdita(lngIndice).NumeroSerie);

					}

					colNSerieOrg.Add(Linha.NumerosSerie.GetEdita(lngIndice).IdNumeroSerie, Linha.NumerosSerie.GetEdita(lngIndice).IdNumeroSerie + "|" + Linha.NumerosSerie.GetEdita(lngIndice).NumeroSerie);

				}

				if (Strings.Len(strIdsOrigem) > 0)
				{

					strSQL = "SELECT TOP " + dblQuantidadeRes.ToString() + " NS.Id, NS.NumeroSerie FROM INV_NumerosSerie (NOLOCK) NS" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN INV_NumerosSerieMovimento NSM (NOLOCK) ON NSM.IdNumeroSerie = NS.Id" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN INV_Movimentos M (NOLOCK) ON M.Id = nsm.IdMovimentoStock" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN INV_Origens    O (NOLOCK) ON O.Id = M.IdOrigem" + Environment.NewLine;
					strSQL = strSQL + "WHERE O.IdChave2 IN (" + strIdsOrigem.Substring(0, Math.Min(Strings.Len(strIdsOrigem) - 1, strIdsOrigem.Length)) + ") AND M.TipoMovimento = 'E'";
					strSQL = strSQL + "ORDER BY NSM.NumRegisto";
					objLista = m_objErpBSO.Consulta(strSQL);

					dynamic tempRefParam = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam))
					{
						objLista = (StdBE100.StdBELista) tempRefParam;

						Linha.NumerosSerie.RemoveTodos();

						while (!objLista.NoFim())
						{

							objNumSerie = new BasBENumeroSerie();

							objNumSerie.IdNumeroSerie = "" + objLista.Valor("Id");
							objNumSerie.NumeroSerie = "" + objLista.Valor("NumeroSerie");
							if (Strings.Len(objNumSerie.NumeroSerie) == 0)
							{

								if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objNumSerie.IdNumeroSerie, colNSeriePreenchidos))
								{

									objNumSerie.NumeroSerie = (string) colNSeriePreenchidos[objNumSerie.IdNumeroSerie];

								}

							}
							objNumSerie.Modulo = ConstantesPrimavera100.Modulos.Vendas;


							if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objNumSerie.IdNumeroSerie, colNSerieOrg))
							{

								colNSerieOrg.Remove(objNumSerie.IdNumeroSerie);

							}

							Linha.NumerosSerie.Insere(objNumSerie);
							objNumSerie = null;

							objLista.Seguinte();

						}

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam;
					}

				}

				if (Strings.Len(strEstadoRes) > 0)
				{

					strSQL = "SELECT TOP @1@ Id, NumeroSerie FROM INV_NumerosSerie (NOLOCK)" + Environment.NewLine;
					strSQL = strSQL + "WHERE Artigo = '@2@' AND Stock = 1 AND EstadoStock IN (" + strEstadoRes.Substring(0, Math.Min(Strings.Len(strEstadoRes) - 1, strEstadoRes.Length)) + ")" + Environment.NewLine;

					dynamic[] tempRefParam2 = new dynamic[]{dblQuantidadeDisp, Linha.Artigo};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam2);

					if (Strings.Len(Linha.Armazem) > 0)
					{

						strSQL = strSQL + " AND Armazem = '@1@'";
						dynamic[] tempRefParam3 = new dynamic[]{Linha.Armazem};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);

					}

					if (Strings.Len(Linha.Localizacao) > 0)
					{

						strSQL = strSQL + " AND Localizacao = '@1@'";
						dynamic[] tempRefParam4 = new dynamic[]{Linha.Localizacao};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam4);

					}

					if (Strings.Len(Linha.Lote) > 0 && Linha.Lote != ConstantesPrimavera100.Inventario.LotePorDefeito)
					{

						strSQL = strSQL + " AND Lote = '@1@'";
						dynamic[] tempRefParam5 = new dynamic[]{Linha.Lote};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam5);

					}


					strSQL = strSQL + "ORDER BY NumRegisto";

					objLista = m_objErpBSO.Consulta(strSQL);

					dynamic tempRefParam6 = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam6))
					{
						objLista = (StdBE100.StdBELista) tempRefParam6;

						Linha.NumerosSerie.RemoveTodos();
						while (!objLista.NoFim())
						{

							objNumSerie = new BasBENumeroSerie();

							objNumSerie.IdNumeroSerie = "" + objLista.Valor("Id");
							objNumSerie.NumeroSerie = "" + objLista.Valor("NumeroSerie");
							if (Strings.Len(objNumSerie.NumeroSerie) == 0)
							{

								if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objNumSerie.IdNumeroSerie, colNSeriePreenchidos))
								{

									objNumSerie.NumeroSerie = (string) colNSeriePreenchidos[objNumSerie.IdNumeroSerie];

								}

							}
							objNumSerie.Modulo = ConstantesPrimavera100.Modulos.Vendas;


							if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objNumSerie.IdNumeroSerie, colNSerieOrg))
							{

								colNSerieOrg.Remove(objNumSerie.IdNumeroSerie);

							}

							Linha.NumerosSerie.Insere(objNumSerie);
							objNumSerie = null;

							objLista.Seguinte();

						}

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam6;
					}

				}

				//Verificar se tinhamos mais números de série do que aqueles que foram inseridos
				if (Linha.NumerosSerie.NumItens < lngNumNSerieAntes)
				{

					lngDifNumSerie = lngNumNSerieAntes - Linha.NumerosSerie.NumItens;
					for (int lngIndice = 1; lngIndice <= lngDifNumSerie; lngIndice++)
					{

						objNumSerie = new BasBENumeroSerie();

						objNumSerie.IdNumeroSerie = Strings.Split((string) colNSerieOrg[lngIndice - 1], "|", -1, CompareMethod.Text)[0];
						objNumSerie.NumeroSerie = Strings.Split((string) colNSerieOrg[lngIndice - 1], "|", -1, CompareMethod.Text)[1];
						objNumSerie.Modulo = ConstantesPrimavera100.Modulos.Vendas;


						Linha.NumerosSerie.Insere(objNumSerie);
						objNumSerie = null;

					}

				}

				objLista = null;
				colNSeriePreenchidos = null;
				colNSerieOrg = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_PreencheNumerosSerieDocsReserva", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : ValidaActualizacaoLinhaOrigTransf
		// Description :
		// Arguments   : objCamposLinhaOrig -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private bool ValidaActualizacaoLinhaOrigTransf(StdBE100.StdBECampos objCamposLinhaOrig)
		{
			bool result = false;
			VndBE100.VndBEDocumentoVenda objDocumento = null;
			BasBESerie objSerie = null;
			VndBE100.VndBETabVenda objTipoDoc = null;

			try
			{

				result = true;

				if (objCamposLinhaOrig != null)
				{

					//Para evitar acessos à base de dados
					objDocumento = new VndBE100.VndBEDocumentoVenda();
					objSerie = new BasBESerie();
					objTipoDoc = new VndBE100.VndBETabVenda();

					string tempRefParam = "DataCarga";
					objDocumento.CargaDescarga.DataCarga = "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam).Valor); //PriGlobal: IGNORE
					string tempRefParam2 = "PaisEntrega";
					objDocumento.CargaDescarga.PaisEntrega = "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam2).Valor); //PriGlobal: IGNORE
					string tempRefParam3 = "ATDocCodeID";
					objDocumento.CargaDescarga.ATDocCodeID = "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam3).Valor); //PriGlobal: IGNORE
					string tempRefParam4 = "TipoEntidade";
					objDocumento.TipoEntidade = "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam4).Valor); //PriGlobal: IGNORE
					string tempRefParam5 = "Entidade";
					objDocumento.Entidade = "" + ReflectionHelper.GetPrimitiveValue<string>(objCamposLinhaOrig.GetItem(ref tempRefParam5).Valor); //PriGlobal: IGNORE

					string tempRefParam6 = "TipoComunicacao";
					objSerie.TipoComunicacao = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposLinhaOrig.GetItem(ref tempRefParam6)); //PriGlobal: IGNORE
					string tempRefParam7 = "TipoDocumento";
					objTipoDoc.TipoDocumento = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposLinhaOrig.GetItem(ref tempRefParam7)); //PriGlobal: IGNORE
					string tempRefParam8 = "BensCirculacao";
					objTipoDoc.BensCirculacao = m_objErpBSO.DSO.Plat.Utils.FBool(objCamposLinhaOrig.GetItem(ref tempRefParam8)); //PriGlobal: IGNORE
					string tempRefParam9 = "PagarReceber";
					objTipoDoc.PagarReceber = m_objErpBSO.DSO.Plat.Utils.FStr(objCamposLinhaOrig.GetItem(ref tempRefParam9)); //BID 588729 'PriGlobal: IGNORE

					//Se não tem Codigo AT, valida-se se devia ter sido comunicado...
					if (Strings.Len(objDocumento.CargaDescarga.ATDocCodeID) == 0)
					{

						if (FuncoesComuns100.FuncoesBS.Documentos.DocumentoTransporteComunicacaoAT(objDocumento, ConstantesPrimavera100.Modulos.Vendas, objSerie, objTipoDoc))
						{

							result = false;

						}

					}

				}

				objDocumento = null;
				objSerie = null;
				objTipoDoc = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValidaActualizacaoLinhaOrigTransf", excep.Message);
			}

			return result;
		}


		// CR.714 - Devolve True se o artigo for o definido como Auto nos Parametros_COP
		private bool ArtigoAutoCCOP(string AutoID, string IdObra, ref string Artigo, ref string TipoDoc)
		{
			bool result = false;
			StdBE100.StdBELista objLista = null;
			string strSQL = "";

			try
			{

				result = false;

				if (Strings.Len(AutoID) > 0 && Strings.Len(IdObra) > 0)
				{
					//BID: 540169
					//strSQL = "SELECT COUNT(ID) FROM COP_Parametros WHERE AUTArtigo = '@1@' AND (AUTDocVendaReceber = '@2@' OR AUTDocVendaPagar = '@3@')"
					strSQL = "SELECT TOP 1 1 FROM COP_Parametros WITH (NOLOCK) WHERE AUTArtigo = '@1@' AND (AUTDocVendaReceber = '@2@' OR AUTDocVendaPagar = '@3@')";
					//END BID: 540169
					dynamic[] tempRefParam = new dynamic[]{Artigo, TipoDoc, TipoDoc};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

					objLista = m_objErpBSO.Consulta(strSQL);

					if (objLista.NumLinhas() > 0)
					{
						result = true;
					}
				}

				objLista = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ArtigoAutoCCOP", excep.Message);
			}

			return result;
		}
		//^CR.714

		//Verifica se existe a referência para o cliente.
		public bool ExisteReferencia(string Filial, string TipoDoc, string Serie, string Entidade, string Referencia)
		{


			return m_objErpBSO.DSO.Vendas.Documentos.ExisteReferencia(ref Filial, ref TipoDoc, ref Serie, ref Entidade, ref Referencia);

			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ExisteReferencia", Information.Err().Description);
			return false;
		}

		public void Remove(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{

			StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IVndBSVendas_ValidaRemocao", "Este método foi descontinuado. Deverá ser utilizado o método _Anulacao()."); //PriGlobal: IGNORE

		}

		//<CT DIMENSOES>
		public VndBELinhasDocumentoVenda EditaLinhasDimensao(string IdLinhaPai)
		{
			//'##SUMMARY Edita as linhas de dimensões associada a uma linha do documento de venda.
			//'##PARAM IdLinhasDoc Identificador da linha do documento de venda.
			//
			//  On Error GoTo ERRO
			//
			//  Set IVndBSVendas_EditaLinhasDim = m_objErpBSO.DSO.Vendas.Documentos.EditaLinhasDim(IdLinhasDoc)
			//
			//  Exit Function
			//
			//ERRO:
			//  Set IVndBSVendas_EditaLinhasDim = Nothing
			//  StdRaiseErro Err.Number, "_VNDBSVendas.EditaLinhasDim", Err.Description
			return null;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ValidaAnulacaoDocumento
		// Description :
		// Arguments   : Filial   -->
		// Arguments   : TipoDoc  -->
		// Arguments   : strSerie -->
		// Arguments   : NumDoc   -->
		// Arguments   : Erros    -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool ValidaAnulacaoDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc, string Erros)
		{

			string tempRefParam = "Id";
			string strIdDoc = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.Vendas.Documentos.DaValorAtributo(Filial, TipoDoc, strSerie, Numdoc, tempRefParam));
			return m_objErpBSO.Vendas.Documentos.ValidaAnulacaoDocumentoID(strIdDoc, Erros);

		}

		public bool ValidaAnulacaoDocumento(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{
			string tempRefParam141 = "";
			return ValidaAnulacaoDocumento(Filial, TipoDoc, strSerie, Numdoc, tempRefParam141);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_ValidaAnulacaoDocumentoID
		// Description :
		// Arguments   : Id    -->
		// Arguments   : Erros -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool ValidaAnulacaoDocumentoID(string Id, string Erros)
		{

			try
			{


				return ValidaAnulacaoDocumentoID(Id, "", Erros);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_ValidaAnulacaoDocumentoID", excep.Message);
			}
			return false;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : ValidaAnulacaoDocumentoID
		// Description :
		// Arguments   : Id    -->
		// Arguments   : Erros -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private bool ValidaAnulacaoDocumentoID(string Id, string Motivo, string Erros)
		{
			bool result = false;
			string strFilial = "";
			string strTipoDoc = "";
			string strSerie = "";
			int lngNumDoc = 0;
			//--------------------------
			int intAnoCB = 0;
			string strDiario = "";
			int lngNumDiario = 0;
			System.DateTime dtDataCBL = DateTime.FromOADate(0);
			int intEstadoCBL = 0;
			string strErrosCBL = "";

			StdBE100.StdBECampos objCampos = null;

			//BID 581368
			int Versao = 0;
			//Fim 581368
			string strATDocCodeId = "";
			string strPagarReceber = ""; //BID 585952

			try
			{

				result = true;

				//BID 585952 (foram adicionados os campos "DataDescarga" e "HoraDescarga")
				//BID (9.05): 595740 - Adicionado o campo "TipoLancamento"
				dynamic[] tempRefParam = new dynamic[]{"Filial", "TipoDoc", "Serie", "NumDoc", "DataCarga", "HoraCarga", "DataDescarga", "HoraDescarga", "Versao", "TipoLancamento"};
				objCampos = m_objErpBSO.Vendas.Documentos.DaValorAtributosID(Id, tempRefParam);

				if (objCampos != null)
				{

					string tempRefParam2 = "Filial";
					strFilial = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam2));
					string tempRefParam3 = "TipoDoc";
					strTipoDoc = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam3));
					string tempRefParam4 = "Serie";
					strSerie = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam4));
					string tempRefParam5 = "NumDoc";
					lngNumDoc = m_objErpBSO.DSO.Plat.Utils.FLng(objCampos.GetItem(ref tempRefParam5));

				}

				//Validação de FILIAIS
				//CS.3943
				if (m_objErpBSO.Base.Filiais.LicencaDeFilial)
				{

					//Na sede pode anular documentos de qualquer filial
					if (m_objErpBSO.Base.Filiais.CodigoFilial != "000")
					{

						if (strFilial != m_objErpBSO.Base.Filiais.CodigoFilial)
						{

							if (m_objErpBSO.Base.Filiais.BDFiliaisDisponivel)
							{

								if (!m_objErpBSO.Base.Filiais.ETerminal(strFilial))
								{

									result = false;
									string tempRefParam6 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9682, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
									dynamic[] tempRefParam7 = new dynamic[]{strTipoDoc};
									Erros = Erros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9065, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam6, tempRefParam7);

								}

							}
							else
							{

								result = false;

								string tempRefParam8 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9682, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam9 = new dynamic[]{strTipoDoc};
								string tempRefParam10 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9643, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam11 = new dynamic[]{m_objErpBSO.DSO.Plat.DefinicaoBD.NomeServidor()};
								Erros = Erros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9065, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam8, tempRefParam9) + 
								        Environment.NewLine + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam10, tempRefParam11);

							}

						}

						//UPGRADE_WARNING: (1068) m_objErpBSO.DSO.Base.Filiais.DaAtributoTimesTamp() of type Variant is being forced to int. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						Versao = ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.DSO.Base.Filiais.DaAtributoTimesTamp("SELECT VersaoUltAct As Versao FROM CabecDoc WITH (NOLOCK) WHERE Filial='" + strFilial + "' AND Serie='" + strSerie + "' AND TipoDoc='" + strTipoDoc + "' AND NumDoc=" + lngNumDoc.ToString())); //PriGlobal: IGNORE
						//FIL A Versão vai ser -1 quando o registo não existir na bd. Solução encontrada para contornar o "problema" da validação
						//ser chamada depois da remoção do documento no Actualiza
						if (Versao != -1)
						{

							if (ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.DSO.Base.Filiais.TSUltimaExportacao("CabecDoc")) > Versao)
							{ //PriGlobal: IGNORE

								result = false;
								string tempRefParam12 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16607, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam13 = new dynamic[]{strTipoDoc, lngNumDoc, strSerie};
								Erros = Erros + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam12, tempRefParam13) + Environment.NewLine;

							}

						}

					}

				}

				//Valida se o utilizador indicou um motivo de estorno
				if (Strings.Len(Motivo) > 0)
				{

					if (~ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.MotivosEstorno.Existe(Motivo)) != 0)
					{

						result = false;
						Erros = Erros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(14669, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}

				}

				//Restantes validações - SP
				if (Strings.Len(Erros) == 0)
				{

					result = result && m_objErpBSO.DSO.Vendas.Documentos.ValidaAnulacaoDocumentoID(Id, ref Erros);

				}

				//Valida se a hora de carga ainda nao foi ultrapassada
				if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
				{

					//UPGRADE_WARNING: (1068) m_objErpBSO.Vendas.Documentos.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					string tempRefParam14 = "ATDocCodeID";
					strATDocCodeId = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.Documentos.DaValorAtributo(strFilial, strTipoDoc, strSerie, lngNumDoc, tempRefParam14));

					if (Strings.Len(strATDocCodeId) > 0)
					{

						//UPGRADE_WARNING: (1068) m_objErpBSO.Vendas.TabVendas.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						string tempRefParam15 = "PagarReceber";
						strPagarReceber = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(strTipoDoc, tempRefParam15)); //BID 585952

						//Se a data/hora de carga é inferior à data/hora atual menso 1 min, nao deixa anular
						//BID 585952
						//If m_objErpBSO.DSO.Plat.Utils.FData(objCampos("DataCarga") & " " & objCampos("HoraCarga")) <= DateAdd("n", 1, Now) Then
						string tempRefParam16 = "DataCarga";
						string tempRefParam17 = "HoraCarga";
						string tempRefParam18 = "Versao";
						string tempRefParam19 = "DataDescarga";
						string tempRefParam20 = "HoraDescarga";
						string tempRefParam21 = "Versao";
						if ((m_objErpBSO.DSO.Plat.Utils.FData(ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam16).Valor) + " " + ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam17).Valor)) <= DateTime.Now.AddMinutes(1) && !FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.InverteCargaDescargaBD("" + ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam18).Valor), strPagarReceber == "P")) || (m_objErpBSO.DSO.Plat.Utils.FData(ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam19).Valor) + " " + ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam20).Valor)) <= DateTime.Now.AddMinutes(1) && FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.InverteCargaDescargaBD("" + ReflectionHelper.GetPrimitiveValue<string>(objCampos.GetItem(ref tempRefParam21).Valor), strPagarReceber == "P")))
						{

							//Fim 585952

							result = false;
							Erros = Erros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16663, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

						}

					}

				}

				//CS.4214 - Planos de Pagamentos
				//        If clsTabVenda.LigacaoCC Then

				if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.PlanosPagamentos.ExistePlanoPagamentosDocId(Id)))
				{

					result = false;
					Erros = Erros + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17163, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}

				//        End If

				//Validação da anulação do documentos da CBL
				if (Strings.Len(Erros) == 0)
				{

					//Obtem o estado actual da integração na CBL
					string tempRefParam22 = ConstantesPrimavera100.Modulos.Vendas;
					m_objErpBSO.Base.LigacaoCBL.BDDevolveDiarioNumero(ref strFilial, ref tempRefParam22, ref strTipoDoc, ref strSerie, ref lngNumDoc, ref intAnoCB, ref strDiario, ref lngNumDiario, ref dtDataCBL, ref intEstadoCBL);

					//Se tem integração feita, então valida a sua anulação...
					if (intAnoCB > 0 && Strings.Len(strDiario) > 0 && lngNumDiario > 0 && (intEstadoCBL == 1 || intEstadoCBL == 3))
					{

						string tempRefParam23 = "TipoLancamento";
						result = (((result) ? -1 : 0) & Convert.ToInt32(m_objErpBSO.Contabilidade.Documentos.ValidaRemocao(intAnoCB, strDiario, lngNumDiario, strErrosCBL, m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam23))))) != 0; //BID (9.05): 595740 - Adicionado o campo "TipoLancamento"
						Erros = Erros + strErrosCBL;

					}

				}

				//Se é uma encomenda e está em edição vamos verificar se está numa necessidade de compra e se é possível _
				//remover essa necessidade
				//UPGRADE_WARNING: (6021) Casting 'Variant' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				string tempRefParam24 = "TipoDocumento";
				if (((BasBETipos.LOGTipoDocumento) ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(strTipoDoc, tempRefParam24))) == BasBETipos.LOGTipoDocumento.LOGDocEncomenda)
				{

					if (~Convert.ToInt32(m_objErpBSO.Compras.PlaneamentoCompras.ProcessaAlteracaoNecessidade(Id, "", Erros)) != 0)
					{

						result = false;

					}

				}

				objCampos = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_ValidaAnulacaoDocumentoID", excep.Message);
			}

			return result;
		}

		public bool ValidaRemocao(string Filial, string TipoDoc, string strSerie, int Numdoc, string StrErro)
		{

			StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IVndBSVendas_ValidaRemocao", "Este método foi descontinuado. Deverá ser utilizado o método _Anulacao()."); //PriGlobal: IGNORE

			return false;
		}

		public bool Existe(string Filial, string TipoDoc, string strSerie, int Numdoc)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.Existe(ref Filial, ref TipoDoc, ref strSerie, ref Numdoc);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Existe", excep.Message);
			}
			return false;
		}
		public bool ExisteID(string Id)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.ExisteID(ref Id);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ExisteID", excep.Message);
			}
			return false;
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin,  string Artigo,  string Cliente,  string NumSerie)
		{


			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.LstLinhasDocVendas( QueryTipo,  DataIni,  DataFin,  Artigo,  Cliente,  NumSerie);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstLinhasDocVendas", excep.Message);
			}
			return null;
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin,  string Artigo,  string Cliente)
		{
			string tempParam145 = "";
			return LstLinhasDocVendas( QueryTipo,  DataIni,  DataFin,  Artigo,  Cliente,  tempParam145);
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin,  string Artigo)
		{
			string tempParam146 = "";
			string tempParam147 = "";
			return LstLinhasDocVendas( QueryTipo,  DataIni,  DataFin,  Artigo,  tempParam146,  tempParam147);
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin)
		{
			string tempParam148 = "";
			string tempParam149 = "";
			string tempParam150 = "";
			return LstLinhasDocVendas( QueryTipo,  DataIni,  DataFin,  tempParam148,  tempParam149,  tempParam150);
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo,  System.DateTime DataIni)
		{
			System.DateTime tempParam151 = DateTime.FromOADate(0);
			string tempParam152 = "";
			string tempParam153 = "";
			string tempParam154 = "";
			return LstLinhasDocVendas( QueryTipo,  DataIni,  tempParam151,  tempParam152,  tempParam153,  tempParam154);
		}

		public StdBELista LstLinhasDocVendas( string QueryTipo)
		{
			System.DateTime tempParam155 = DateTime.FromOADate(0);
			System.DateTime tempParam156 = DateTime.FromOADate(0);
			string tempParam157 = "";
			string tempParam158 = "";
			string tempParam159 = "";
			return LstLinhasDocVendas( QueryTipo,  tempParam155,  tempParam156,  tempParam157,  tempParam158,  tempParam159);
		}

		public StdBELista LstLinhasDocVendas()
		{
			string tempParam160 = "";
			System.DateTime tempParam161 = DateTime.FromOADate(0);
			System.DateTime tempParam162 = DateTime.FromOADate(0);
			string tempParam163 = "";
			string tempParam164 = "";
			string tempParam165 = "";
			return LstLinhasDocVendas( tempParam160,  tempParam161,  tempParam162,  tempParam163,  tempParam164,  tempParam165);
		}
		public StdBELista LstLinhasDocVendasNumerosSerie( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin,  string Artigo,  string Cliente)
		{

			try
			{
				return m_objErpBSO.DSO.Vendas.Documentos.LstLinhasDocVendasNumerosSerie( QueryTipo,  DataIni,  DataFin,  Artigo,  Cliente);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstLinhasDocVendasNumerosSerie", excep.Message);
			}
			return null;
		}

		public StdBELista LstLinhasDocVendasNumerosSerie( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin,  string Artigo)
		{
			string tempParam166 = "";
			return LstLinhasDocVendasNumerosSerie( QueryTipo,  DataIni,  DataFin,  Artigo,  tempParam166);
		}

		public StdBELista LstLinhasDocVendasNumerosSerie( string QueryTipo,  System.DateTime DataIni,  System.DateTime DataFin)
		{
			string tempParam167 = "";
			string tempParam168 = "";
			return LstLinhasDocVendasNumerosSerie( QueryTipo,  DataIni,  DataFin,  tempParam167,  tempParam168);
		}

		public StdBELista LstLinhasDocVendasNumerosSerie( string QueryTipo,  System.DateTime DataIni)
		{
			System.DateTime tempParam169 = DateTime.FromOADate(0);
			string tempParam170 = "";
			string tempParam171 = "";
			return LstLinhasDocVendasNumerosSerie( QueryTipo,  DataIni,  tempParam169,  tempParam170,  tempParam171);
		}

		public StdBELista LstLinhasDocVendasNumerosSerie( string QueryTipo)
		{
			System.DateTime tempParam172 = DateTime.FromOADate(0);
			System.DateTime tempParam173 = DateTime.FromOADate(0);
			string tempParam174 = "";
			string tempParam175 = "";
			return LstLinhasDocVendasNumerosSerie( QueryTipo,  tempParam172,  tempParam173,  tempParam174,  tempParam175);
		}

		public StdBELista LstLinhasDocVendasNumerosSerie()
		{
			string tempParam176 = "";
			System.DateTime tempParam177 = DateTime.FromOADate(0);
			System.DateTime tempParam178 = DateTime.FromOADate(0);
			string tempParam179 = "";
			string tempParam180 = "";
			return LstLinhasDocVendasNumerosSerie( tempParam176,  tempParam177,  tempParam178,  tempParam179,  tempParam180);
		}

		//Preenche os dados relativos à prestação
		private VndBE100.VndBEDocumentoVenda PreencheDadosPrestacao(ref VndBE100.VndBEDocumentoVenda clsDocVenda)
		{
			BasBECondPagamento clsCondPag = null;
			BasBEPrestacao ClsPrestacao = null;
			double Valor = 0;
			double valorIni = 0;
			double ValorTotal = 0;
			double TotalPrestacoes = 0;
			System.DateTime Data = DateTime.FromOADate(0);
			int NumPrest = 0;
			int Mult = 0;

			try
			{

				//Se o valor do documento é nulo então não preenche os dados da prestação
				if (clsDocVenda.TotalDocumento == 0)
				{
					return null;
				}

				//Remover as prestações para as voltar a inserir
				for (int i = clsDocVenda.Prestacoes.NumItens; i >= 1; i--)
				{
					clsDocVenda.Prestacoes.Remove(i);
				}

				//Se o documento for uma prestacao
				if (Strings.Len(clsDocVenda.CondPag) == 0)
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSCompras.PreencheDadosPrestacao", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9048, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				clsCondPag = m_objErpBSO.Base.CondsPagamento.Edita(clsDocVenda.CondPag);

				//Verificar se a condição de pagamento é uma prestação
				if (clsCondPag.TipoCondicao == "4")
				{

					//Verificar o valor para cada prestação
					CalculaTotaisDocumento(ref clsDocVenda);
					ValorTotal = clsDocVenda.TotalDocumento;

					string tempRefParam = clsDocVenda.Tipodoc;
					string tempRefParam2 = "PagarReceber";
					if (ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam, tempRefParam2)) == "R")
					{
						Mult = 1;
					}
					else
					{
						Mult = -1;
					}
					ValorTotal = Mult * ValorTotal;

					Data = clsDocVenda.DataDoc;

					if (ValorTotal != 0)
					{
						//Criar uma prestação com o valor da entrada inicial
						if (clsCondPag.EntradaInicial != 0)
						{
							//Inicializar os valores
							valorIni = Math.Round((double) (ValorTotal * clsCondPag.EntradaInicial / 100), clsDocVenda.Arredondamento);
							//O pagamento da entrada inicial tem que ser feito até x dias depois
							if (clsCondPag.DiasVencimentoEntradaInicial != 0)
							{
								System.DateTime tempRefParam3 = clsDocVenda.DataDoc;
								string tempRefParam4 = clsDocVenda.CondPag;
								int tempRefParam5 = clsCondPag.DiasVencimentoEntradaInicial;
								string tempRefParam6 = clsDocVenda.TipoEntidade;
								string tempRefParam7 = clsDocVenda.Entidade;
								Data = CalculaDataVencimento(tempRefParam3, tempRefParam4, tempRefParam5, tempRefParam6, tempRefParam7);
							}
							NumPrest = 1;
							TotalPrestacoes += valorIni;

							ClsPrestacao = new BasBEPrestacao();

							ClsPrestacao.DataVenc = Data;
							ClsPrestacao.NumPrestacao = NumPrest;
							ClsPrestacao.Valor = valorIni;

							clsDocVenda.Prestacoes.Insere(ClsPrestacao);

							ClsPrestacao = null;
						}

						if (clsCondPag.NumeroPrestacoes > 0)
						{
							Valor = Math.Round((double) ((ValorTotal - valorIni) / clsCondPag.NumeroPrestacoes), clsDocVenda.Arredondamento);
						}

						//Criar vários documentos pendentes consoante o número de prestações
						int tempForVar = clsCondPag.NumeroPrestacoes;
						for (int i = 1; i <= tempForVar; i++)
						{
							//Calcular a data de vencimento
							if (i == 1)
							{

								//BID 17540 : foi adicionado este cenário da 1ª prestação
								string tempRefParam8 = clsCondPag.CondPag;
								int tempRefParam9 = 0;
								string tempRefParam10 = clsDocVenda.TipoEntidade;
								string tempRefParam11 = clsDocVenda.Entidade;
								Data = CalculaDataVencimento(Data, tempRefParam8, tempRefParam9, tempRefParam10, tempRefParam11);

							}
							else
							{

								//BID 528309 (foi adicionado o parâmetro "clsCondPag.PeriodicidadePrestacoes")
								string tempRefParam12 = clsCondPag.CondPag;
								int tempRefParam13 = clsCondPag.PeriodicidadePrestacoes;
								string tempRefParam14 = clsDocVenda.TipoEntidade;
								string tempRefParam15 = clsDocVenda.Entidade;
								Data = CalculaDataVencimento(Data, tempRefParam12, tempRefParam13, tempRefParam14, tempRefParam15);

							}


							//Faz o total das prestações
							if (i == clsCondPag.NumeroPrestacoes)
							{
								Valor = ValorTotal - TotalPrestacoes;
							}
							TotalPrestacoes += Valor;

							ClsPrestacao = new BasBEPrestacao();

							ClsPrestacao.DataVenc = Data;
							ClsPrestacao.NumPrestacao = NumPrest + i;
							ClsPrestacao.Valor = Valor;
							clsDocVenda.Prestacoes.Insere(ClsPrestacao);

							ClsPrestacao = null;
						}
					}

				}



				return clsDocVenda;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosPrestacao", excep.Message);
			}
			return null;
		}

		//---------------------------------------------------------------------------------------
		// Procedure     : PreencheDadosContrato
		// Description   :
		//---------------------------------------------------------------------------------------
		private VndBE100.VndBEDocumentoVenda PreencheDadosContrato(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{

			return (VndBE100.VndBEDocumentoVenda) Contratos.PreencheDadosContrato(clsDocVenda);

		}

		// Os dados da morada da entidade nos documentos criados a partir de avenças
		// deve ser a morada definida no documento original e não a morada por defeito da entidade
		public VndBEDocumentoVenda PreencheDadosRelacionadosEntidadeAvencas(string FilialOriginal, string TipoDocOriginal, string SerieOriginal, int NumDocOriginal, VndBEDocumentoVenda clsDocVenda)
		{
			try
			{

				if (clsDocVenda.TipoEntidade == "")
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.PreencheDadosRelacionados", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9808, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}
				if (!m_objErpBSO.Base.Series.Existe("V", clsDocVenda.Tipodoc, clsDocVenda.Serie))
				{
					string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9809, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam2 = new dynamic[]{clsDocVenda.Serie, clsDocVenda.Tipodoc};
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.PreencheDadosRelacionados", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2));
				}

				PreencheDadosEntidadeAvencas(ref FilialOriginal, ref TipoDocOriginal, ref SerieOriginal, ref NumDocOriginal, clsDocVenda);

				//Alteração para permitir que seja invocada a função sem condição de pagamento (por exemplo) associada ao cliente
				if (Strings.Len(clsDocVenda.CondPag) != 0)
				{
					PreencheDadosCondPag(clsDocVenda);
				}
				else
				{
					LimpaDadosCondPag(clsDocVenda);
				}
				if (Strings.Len(clsDocVenda.Moeda) != 0)
				{
					PreencheDadosMoeda(clsDocVenda);
				}
				else
				{
					LimpaDadosMoeda(clsDocVenda);
				}


				return clsDocVenda;
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosRelacionadosEntidade", excep.Message);
			}
			return null;
		}

		public VndBEDocumentoVenda PreencheDadosRelacionados(VndBEDocumentoVenda clsDocVenda, int Preenche)
		{
			string strCondPagDoc = ""; //BID 531161

			try
			{

				if (Preenche == -1)
				{

					Preenche = (short) BasBETiposGcp.PreencheRelacaoVendas.vdDadosTodos;

				}

				if (clsDocVenda.TipoEntidade == "")
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.PreencheDadosRelacionados", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9808, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				if (Strings.Len(clsDocVenda.Serie) > 0)
				{ //BID 569744
					if (!m_objErpBSO.Base.Series.Existe("V", clsDocVenda.Tipodoc, clsDocVenda.Serie))
					{
						string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9809, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam2 = new dynamic[]{clsDocVenda.Serie, clsDocVenda.Tipodoc};
						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.PreencheDadosRelacionados", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2));
					}
				}

				//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				switch((BasBETiposGcp.PreencheRelacaoVendas) Preenche)
				{
					//Através do tipo do documento devolve a data e o número do documento
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosTipoDoc : 
						PreencheDadosTipoDoc(clsDocVenda); 
						 
						//Através da moeda devolve o câmbio da moeda 
						break;
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosMoeda : 
						PreencheDadosMoeda(clsDocVenda); 
						 
						//Através do cliente devolve a informação do cliente 
						break;
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosCliente : 
						PreencheDadosEntidade(clsDocVenda); 
						//Alteração para permitir que seja invocada a função sem condição de pagamento (por exemplo) associada ao cliente 
						if (Strings.Len(clsDocVenda.CondPag) != 0)
						{
							PreencheDadosCondPag(clsDocVenda);
						}
						else
						{
							LimpaDadosCondPag(clsDocVenda);
						} 
						if (Strings.Len(clsDocVenda.Moeda) != 0)
						{
							PreencheDadosMoeda(clsDocVenda);
						}
						else
						{
							LimpaDadosMoeda(clsDocVenda);
						} 
						 
						//Através da condição de pagamento devolve toda a informação relacionada com a condição de pagamento 
						break;
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosCondPag : 
						PreencheDadosCondPag(clsDocVenda); 
						 
						break;
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosPrestacao : 
						PreencheDadosPrestacao(ref clsDocVenda); 
						 
						//        'Epic 406 
						break;
					case BasBETiposGcp.PreencheRelacaoVendas.vdDadosContrato : 
						PreencheDadosContrato(clsDocVenda); 
						 
						//Devolve toda a informação relacionada com o cabeçalho do documento 
						break;
					default:
						FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(clsDocVenda.CamposUtil, DaDefCamposUtil()); 
						 
						PreencheDadosTipoDoc(clsDocVenda); 
						strCondPagDoc = clsDocVenda.CondPag;  //BID 531161 
						PreencheDadosEntidade(clsDocVenda); 
						if (Strings.Len(strCondPagDoc) > 0)
						{
							clsDocVenda.CondPag = strCondPagDoc;
						}  //BID 531161 
						if (Strings.Len(clsDocVenda.CondPag) != 0)
						{
							PreencheDadosCondPag(clsDocVenda);
						}
						else
						{
							LimpaDadosCondPag(clsDocVenda);
						} 
						if (Strings.Len(clsDocVenda.Moeda) != 0)
						{
							PreencheDadosMoeda(clsDocVenda);
						}
						else
						{
							//Por defeito fica a moeda base
							clsDocVenda.Moeda = m_objErpBSO.Contexto.MoedaBase;
							PreencheDadosMoeda(clsDocVenda);
						} 
						 
						//Epic 406 
						PreencheDadosContrato(clsDocVenda); 
						 
						break;
				}


				return clsDocVenda;
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosRelacionados", excep.Message);
			}
			return null;
		}

		public VndBEDocumentoVenda PreencheDadosRelacionados(VndBEDocumentoVenda clsDocVenda)
		{
			int tempRefParam181 = -1;
			return PreencheDadosRelacionados(clsDocVenda, tempRefParam181);
		}

		public BasBEMoradaAlternativa ObtemMoradaDocumentoOriginal(string Filial, string TipoDoc, string Serie, int Numdoc)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.ObtemMoradaDocumentoOriginal(ref Filial, ref TipoDoc, ref Serie, ref Numdoc);
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ObtemMoradaDocumentoOriginal", excep.Message);
			}
			return null;
		}

		private VndBE100.VndBEDocumentoVenda PreencheDadosTipoDoc(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{
			VndBE100.VndBEDocumentoVenda result = null;
			VndBE100.VndBETabVenda clsTabVenda = null;
			BasBESerie objSerie = null;
			StdBE100.StdBELista objLista = null;

			try
			{


				if (Strings.Len(clsDocVenda.Tipodoc) == 0)
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosTipoDoc", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9058, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				//Edita a configuração do documento de venda
				string tempRefParam = clsDocVenda.Tipodoc;
				clsTabVenda = m_objErpBSO.Vendas.TabVendas.Edita(tempRefParam);

				//CS.4054
				clsDocVenda.TrataIvaCaixa = clsTabVenda.DeduzLiquidaIVA;

				clsDocVenda.GeraPendentePorLinha = clsTabVenda.GeraPendentePorLinha;

				if (Strings.Len(clsDocVenda.ID) == 0)
				{
					bool tempRefParam2 = true;
					clsDocVenda.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam2);
				}

				// Caso a série não esteja preenchida, inicializa com a série por defeito.
				if (Strings.Len(clsDocVenda.Serie) == 0)
				{
					clsDocVenda.Serie = m_objErpBSO.Base.Series.DaSerieDefeito("V", clsDocVenda.Tipodoc);
				}

				objSerie = m_objErpBSO.Base.Series.Edita("V", clsDocVenda.Tipodoc, clsDocVenda.Serie);

				//BID:531031
				if (objSerie == null)
				{
					string tempRefParam3 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3013, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam4 = new dynamic[]{clsDocVenda.Serie, clsDocVenda.Tipodoc};
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosTipoDoc", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam3, tempRefParam4));
				}
				//END BID:531031

				//Preenche o numerador do documento consoante a secção
				//O campo secção não existe na tabela de parâmetros
				//.Seccao = m_objErpBSO.Vendas.Params.Seccao
				if (Strings.Len(clsDocVenda.Seccao) == 0)
				{
					clsDocVenda.Seccao = "1";
				}

				//FIL
				clsDocVenda.Filial = m_objErpBSO.Base.Filiais.CodigoFilial;

				//Preenche o mapa, o número de vias e a pré-visualização
				clsDocVenda.MapaImpressao = objSerie.Config;

				if (objSerie.NumVias != 0)
				{
					clsDocVenda.NumVias = objSerie.NumVias;
				}
				else
				{
					clsDocVenda.NumVias = 0;
				}

				clsDocVenda.PreVisualizar = objSerie.Previsao;

				clsDocVenda.NumDoc = m_objErpBSO.Base.Series.ProximoNumero("V", clsDocVenda.Tipodoc, clsDocVenda.Serie, true);
				clsDocVenda.DataDoc = m_objErpBSO.Base.Series.SugereDataDocumento("V", clsDocVenda.Tipodoc, clsDocVenda.Serie);

				//BID 531711
				if (clsTabVenda.SugereCondPag)
				{
					clsDocVenda.CondPag = clsTabVenda.CondPagASugerir; //BID 531161
				}
				//FIM BID 531711

				string tempRefParam5 = clsDocVenda.Tipodoc;
				objLista = m_objErpBSO.Vendas.FluxosVenda.LstFluxos(tempRefParam5);
				if (!objLista.Vazia())
				{
					clsDocVenda.Fluxo = objLista.Valor("Fluxo");
				}
				objLista = null;

				//CS.3866 - Sugere CAE da série; se não existe então sugere o CAE da EMPRESA
				if (Strings.Len(clsDocVenda.CAE) == 0)
				{
					clsDocVenda.CAE = objSerie.CAESugerido;
				}
				if (Strings.Len(clsDocVenda.CAE) == 0)
				{
					clsDocVenda.CAE = m_objErpBSO.Contexto.IFCAE;
				}

				result = clsDocVenda;
				clsTabVenda = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosTipoDoc", excep.Message);
			}


			return result;
		}

		private VndBE100.VndBEDocumentoVenda PreencheDadosMoeda(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{

			VndBE100.VndBEDocumentoVenda result = null;
			BasBEMoeda clsMoeda = null;

			try
			{

				if (Strings.Len(clsDocVenda.Moeda) == 0)
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosMoeda", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9057, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				clsMoeda = m_objErpBSO.Base.Moedas.Edita(clsDocVenda.Moeda);

				clsDocVenda.Cambio = m_objErpBSO.Base.Moedas.DaCambioCompra(clsDocVenda.Moeda, clsDocVenda.DataDoc);

				if (m_objErpBSO.Contexto.MoedaBase == m_objErpBSO.Contexto.MoedaEuro)
				{
					clsDocVenda.CambioMBase = 1;
					clsDocVenda.CambioMAlt = m_objErpBSO.Base.Moedas.DaCambioCompra(m_objErpBSO.Contexto.MoedaAlternativa, clsDocVenda.DataDoc);
				}
				else
				{
					clsDocVenda.CambioMBase = m_objErpBSO.Base.Moedas.DaCambioCompra(m_objErpBSO.Contexto.MoedaBase, clsDocVenda.DataDoc);
					clsDocVenda.CambioMAlt = 1;
				}

				clsDocVenda.MoedaDaUEM = clsMoeda.PertenceUEM;
				clsDocVenda.Arredondamento = (byte) clsMoeda.DecArredonda;
				clsDocVenda.ArredondamentoIva = (byte) clsMoeda.DecArredondaIVA;
				clsDocVenda.SimboloMoeda = clsMoeda.Simbolo;

				result = clsDocVenda;
				clsMoeda = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosMoeda", excep.Message);
			}


			return result;
		}

		private VndBE100.VndBEDocumentoVenda PreencheDadosEntidadeAvencas(ref string FilialOriginal, ref string TipoDocOriginal, ref string SerieOriginal, ref int NumDocOriginal, VndBE100.VndBEDocumentoVenda clsDocVenda)
		{

			VndBE100.VndBEDocumentoVenda result = null;
			dynamic clsEntidade = null;
			BasBEMoradaAlternativa ClsMorada = null;
			BasBEMoradaAlternativa objMorada = null;
			BasBEContaBancariaTerc objConta = null;

			try
			{


				if (Strings.Len(clsDocVenda.Entidade) == 0)
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosCliente", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9051, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				if (clsDocVenda.TipoEntidade == "C")
				{
					clsEntidade = m_objErpBSO.Base.Clientes.Consulta(clsDocVenda.Entidade);
				}
				else
				{
					clsEntidade = m_objErpBSO.Base.OutrosTerceiros.Edita(clsDocVenda.Entidade);
				}

				if (Convert.ToBoolean(clsEntidade.Inactivo))
				{
					string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9080, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam2 = new dynamic[]{clsDocVenda.Entidade};
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2) + Environment.NewLine);
				}

				// Obter os dados da morada do documento original
				objMorada = m_objErpBSO.DSO.Vendas.Documentos.ObtemMoradaDocumentoOriginal(ref FilialOriginal, ref TipoDocOriginal, ref SerieOriginal, ref NumDocOriginal);

				clsDocVenda.Nome = Convert.ToString(clsEntidade.Nome);
				clsDocVenda.Morada = objMorada.Morada;
				clsDocVenda.Morada2 = objMorada.Morada2; //BID 584383
				clsDocVenda.Localidade = objMorada.Localidade;
				clsDocVenda.NumContribuinte = Convert.ToString(clsEntidade.NumContribuinte);
				clsDocVenda.CodigoPostal = objMorada.CodigoPostal;
				clsDocVenda.LocalidadeCodigoPostal = objMorada.LocalidadeCodigoPostal;

				//BID 584383
				//.Distrito = clsEntidade.Distrito
				clsDocVenda.Distrito = objMorada.Distrito;
				clsDocVenda.Pais = objMorada.Pais;
				//Fim 584383

				objConta = m_objErpBSO.Base.ContasBancariasTerceiros.DaContaBancariasDefeito(clsDocVenda.TipoEntidade, clsDocVenda.Entidade);
				if (objConta != null)
				{
					clsDocVenda.ContaDomiciliacao = objConta.Conta;
					//BID 551328
				}
				else
				{
					clsDocVenda.ContaDomiciliacao = "";
					//Fim 551328
				}
				objConta = null;

				clsDocVenda.ModoPag = Convert.ToString(clsEntidade.ModoPag);
				clsDocVenda.CondPag = Convert.ToString(clsEntidade.CondPag);
				clsDocVenda.Moeda = Convert.ToString(clsEntidade.Moeda);
				if (clsDocVenda.TipoEntidade == "C")
				{
					clsDocVenda.ModoExp = Convert.ToString(clsEntidade.ModoExp);
				}

				//CS.1398
				clsDocVenda.RegimeIva = m_objErpBSO.DSO.Plat.Utils.FStr((int) FuncoesComuns100.FuncoesBS.Documentos.DevolveEspacoFiscalCalculado(Convert.ToString(clsEntidade.TipoMercado), Convert.ToInt32(clsEntidade.RegimeIvaReembolsos), m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc, clsDocVenda.Serie, "IvaIncluido")), ConstantesPrimavera100.Modulos.Vendas));

				//.RegimeIvaReembolsos = clsEntidade.RegimeIvaReembolsos
				clsDocVenda.RegimeIvaReembolsos = FuncoesComuns100.FuncoesBS.Documentos.DevolveRegimeIvaReembolsos(Convert.ToInt32(clsEntidade.RegimeIvaReembolsos), ConstantesPrimavera100.Modulos.Vendas, Convert.ToString(clsEntidade.TipoMercado));

				clsDocVenda.EspacoFiscal = (int) FuncoesComuns100.FuncoesBS.Documentos.DevolveIndiceComboEspacoFiscal(Convert.ToString(clsEntidade.TipoMercado), ConstantesPrimavera100.Modulos.Vendas);

				//END CS.1398

				clsDocVenda.LocalOperacao = Convert.ToString(clsEntidade.LocalOperacao);

				if (clsDocVenda.TipoEntidade == "C")
				{
					//.DescEntidade = clsEntidade.Desconto 'BID 521678
					clsDocVenda.Zona = Convert.ToString(clsEntidade.Zona);
				}

				//O responsável pela conbrança corresponde ao vendedor associado ao cliente
				clsDocVenda.Responsavel = Convert.ToString(clsEntidade.Vendedor);

				if (clsDocVenda.TipoEntidade == "C")
				{
					//Verificar qual é a morada alternativa por defeito
					ClsMorada = m_objErpBSO.Base.MoradasAlternativas.DaMoradaAlternativaDefeito(clsDocVenda.TipoEntidade, clsDocVenda.Entidade);
					if (ClsMorada != null)
					{
						clsDocVenda.MoradaAlternativaEntrega = ClsMorada.MoradaAlternativa;
						clsDocVenda.UtilizaMoradaAlternativaEntreg = true;
					}
				}

				//** Parâmetros da aplicação
				clsDocVenda.LocalCarga = m_objErpBSO.Vendas.Params.LocalCarga;
				clsDocVenda.LocalDescarga = m_objErpBSO.Vendas.Params.LocalDescarga;
				clsDocVenda.HoraCarga = Strings.FormatDateTime(DateTime.Now, DateFormat.ShortTime);

				//BID 582491
				clsDocVenda.DataCarga = DateTimeHelper.ToString(DateTime.Today);
				clsDocVenda.DataDescarga = DateTimeHelper.ToString(DateTime.Today);
				//Fim 582491


				if (Strings.Len(clsDocVenda.Seccao) == 0)
				{
					clsDocVenda.Seccao = m_objErpBSO.Vendas.Params.Seccao;
				}

				clsDocVenda.SujeitoRetencao = Convert.ToBoolean(clsEntidade.EfectuaRetencao);
				clsDocVenda.PercentagemRetencao = 0;

				clsDocVenda.SujeitoRecargo = Convert.ToBoolean(clsEntidade.SujeitoRecargo);

				//BID: 567800
				clsDocVenda.CambioADataDoc = Convert.ToBoolean(clsEntidade.CambioADataDoc);

				//CS.4054
				if (!clsDocVenda.TrataIvaCaixa)
				{

					clsDocVenda.TrataIvaCaixa = Convert.ToBoolean(clsEntidade.TrataIvaCaixa);

				}

				result = clsDocVenda;
				clsEntidade = null;
				objMorada = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosEntidadeAvencas", excep.Message);
			}

			return result;
		}

		private VndBE100.VndBEDocumentoVenda PreencheDadosEntidade(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{
			VndBE100.VndBEDocumentoVenda result = null;
			dynamic clsEntidade = null;
			BasBEMoradaAlternativa ClsMorada = null;
			BasBEContaBancariaTerc objConta = null;
			bool blnInactivo = false; //BID: 576188 - CS.3185
			bool blnMorAlt = false;
			//BID 584932
			StdBE100.StdBELista objLista = null;
			string strPais = "";
			//Fim 584932
			string strPagarReceber = "";

			try
			{

				BasBECargaDescarga withVar = null;

				if (Strings.Len(clsDocVenda.Entidade) == 0)
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosCliente", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9051, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));

				}

				//BID: 576188 - CS.3185

				string switchVar = clsDocVenda.TipoEntidade;
				if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
				{

					clsEntidade = m_objErpBSO.Base.Clientes.Consulta(clsDocVenda.Entidade);

				}
				else if (switchVar == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
				{ 

					clsEntidade = m_objErpBSO.Base.OutrosTerceiros.Edita(clsDocVenda.Entidade);

				}
				else if (switchVar == ConstantesPrimavera100.TiposEntidade.EntidadeExterna)
				{ 

					clsEntidade = m_objErpBSO.CRM.EntidadesExternas.Edita(clsDocVenda.Entidade);

				}

				if (clsEntidade == null)
				{
					string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9816, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam2 = new dynamic[]{clsDocVenda.Entidade};
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2) + Environment.NewLine);
				}

				if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente || clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
				{
					blnInactivo = Convert.ToBoolean(clsEntidade.Inactivo);
				}
				else
				{
					blnInactivo = ~Convert.ToInt32(clsEntidade.Activo) != 0;
				}
				//BID: 576188 - CS.3185

				//BID: 576188 - CS.3185
				if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente || clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
				{


					clsDocVenda.Nome = Convert.ToString(clsEntidade.Nome); //BID 14481 : estava "NomeFiscal" em vez de "Nome"
					clsDocVenda.Morada = Convert.ToString(clsEntidade.Morada);
					clsDocVenda.Morada2 = Convert.ToString(clsEntidade.Morada2); //BID 551619
					clsDocVenda.Localidade = Convert.ToString(clsEntidade.Localidade);
					clsDocVenda.NumContribuinte = Convert.ToString(clsEntidade.NumContribuinte);
					clsDocVenda.CodigoPostal = Convert.ToString(clsEntidade.CodigoPostal);
					clsDocVenda.LocalidadeCodigoPostal = Convert.ToString(clsEntidade.LocalidadeCodigoPostal);
					clsDocVenda.Distrito = Convert.ToString(clsEntidade.Distrito);
					clsDocVenda.Pais = Convert.ToString(clsEntidade.Pais); //BID 580239

					//CR.680 - 750 Alfa7
					clsDocVenda.TipoEntidadeFac = clsDocVenda.TipoEntidade; // [BID:537191/540176] Actualmente apenas este tipo é suportado.
					clsDocVenda.EntidadeFac = clsDocVenda.Entidade;
					clsDocVenda.NomeFac = Convert.ToString(clsEntidade.NomeFiscal);
					clsDocVenda.MoradaFac = Convert.ToString(clsEntidade.Morada);
					clsDocVenda.Morada2Fac = Convert.ToString(clsEntidade.Morada2); //BID 551619
					clsDocVenda.LocalidadeFac = Convert.ToString(clsEntidade.Localidade);
					clsDocVenda.NumContribuinteFac = Convert.ToString(clsEntidade.NumContribuinte);
					clsDocVenda.CodigoPostalFac = Convert.ToString(clsEntidade.CodigoPostal);
					clsDocVenda.LocalidadeCodigoPostalFac = Convert.ToString(clsEntidade.LocalidadeCodigoPostal);
					clsDocVenda.DistritoFac = Convert.ToString(clsEntidade.Distrito);
					clsDocVenda.PaisFac = Convert.ToString(clsEntidade.Pais); //BID 580239
					//^CR.680 - 750 Alfa7

					//CS.2243
					clsDocVenda.CambioADataDoc = Convert.ToBoolean(clsEntidade.CambioADataDoc);

					objConta = m_objErpBSO.Base.ContasBancariasTerceiros.DaContaBancariasDefeito(clsDocVenda.TipoEntidade, clsDocVenda.Entidade);

					if (objConta != null)
					{

						clsDocVenda.ContaDomiciliacao = objConta.Conta;

						//BID 551328
					}
					else
					{

						clsDocVenda.ContaDomiciliacao = "";

						//Fim 551328
					}

					objConta = null;

					//BID 545605
					//.ModoPag = clsEntidade.ModoPag
					string tempRefParam3 = clsDocVenda.Tipodoc;
					string tempRefParam4 = "PagarReceber";
					strPagarReceber = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam3, tempRefParam4)).ToUpper();

					if (strPagarReceber == "R")
					{

						clsDocVenda.ModoPag = Convert.ToString(clsEntidade.ModoPag);

					}
					else
					{

						clsDocVenda.ModoPag = Convert.ToString(clsEntidade.ModoRec);

					}

					//Fim 545605
					clsDocVenda.CondPag = Convert.ToString(clsEntidade.CondPag);
					clsDocVenda.Moeda = Convert.ToString(clsEntidade.Moeda);

					if (clsDocVenda.TipoEntidade == "C")
					{

						clsDocVenda.ModoExp = Convert.ToString(clsEntidade.ModoExp);

					}

					//CS.1398
					clsDocVenda.RegimeIva = m_objErpBSO.DSO.Plat.Utils.FStr((int) FuncoesComuns100.FuncoesBS.Documentos.DevolveEspacoFiscalCalculado(Convert.ToString(clsEntidade.TipoMercado), Convert.ToInt32(clsEntidade.RegimeIvaReembolsos), m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc, clsDocVenda.Serie, "IvaIncluido")), ConstantesPrimavera100.Modulos.Vendas));

					//BID: 560303
					//.RegimeIvaReembolsos = clsEntidade.RegimeIvaReembolsos
					clsDocVenda.RegimeIvaReembolsos = FuncoesComuns100.FuncoesBS.Documentos.DevolveRegimeIvaReembolsos(Convert.ToInt32(clsEntidade.RegimeIvaReembolsos), ConstantesPrimavera100.Modulos.Vendas, Convert.ToString(clsEntidade.TipoMercado));
					//^BID: 560303

					clsDocVenda.EspacoFiscal = (int) FuncoesComuns100.FuncoesBS.Documentos.DevolveIndiceComboEspacoFiscal(Convert.ToString(clsEntidade.TipoMercado), ConstantesPrimavera100.Modulos.Vendas);

					//END CS.1398

					clsDocVenda.LocalOperacao = Convert.ToString(clsEntidade.LocalOperacao);

					//BID 579860
					//If .RegimeIva = LOGEspacoFiscalDoc.MercadoIntracomunitario Then
					if (clsDocVenda.EspacoFiscal == ((int) FuncoesComuns100.clsBSEditoresRegimeIVA.GCPEspacoFiscalCombo.GCPEspacoFiscalIntracomunitario))
					{
						//Fim 579860

						clsDocVenda.TipoOperacao = FuncoesComuns100.FuncoesBS.Documentos.DaTipoOperacaoIntracomunitario(ConstantesPrimavera100.Modulos.Vendas);

					}

					if (clsDocVenda.TipoEntidade == "C")
					{

						clsDocVenda.DescEntidade = Convert.ToDouble(clsEntidade.Desconto);
						clsDocVenda.Zona = Convert.ToString(clsEntidade.Zona);

					}

					//O responsável pela conbrança corresponde ao vendedor associado ao cliente
					clsDocVenda.Responsavel = Convert.ToString(clsEntidade.Vendedor);

					//** Parâmetros da aplicação
					clsDocVenda.LocalCarga = m_objErpBSO.Vendas.Params.LocalCarga;
					clsDocVenda.LocalDescarga = m_objErpBSO.Vendas.Params.LocalDescarga;
					clsDocVenda.HoraCarga = Strings.FormatDateTime(DateTime.Now, DateFormat.ShortTime);

					//BID 582491
					clsDocVenda.DataCarga = DateTimeHelper.ToString(DateTime.Today);
					clsDocVenda.DataDescarga = DateTimeHelper.ToString(DateTime.Today);
					//Fim 582491

					if (Strings.Len(clsDocVenda.Seccao) == 0)
					{
						clsDocVenda.Seccao = m_objErpBSO.Vendas.Params.Seccao;
					}

					clsDocVenda.SujeitoRetencao = Convert.ToBoolean(clsEntidade.EfectuaRetencao);
					clsDocVenda.TipoTerceiro = Convert.ToString(clsEntidade.TipoTerceiro);

					clsDocVenda.PercentagemRetencao = 0;

					clsDocVenda.SujeitoRecargo = Convert.ToBoolean(clsEntidade.SujeitoRecargo);

					//CS.4054
					if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
					{

						if ((!clsDocVenda.TrataIvaCaixa) && ((clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente) || (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)))
						{

							clsDocVenda.TrataIvaCaixa = Convert.ToBoolean(clsEntidade.TrataIvaCaixa);

						}

					}
					else if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha)
					{ 

						if (clsDocVenda.TrataIvaCaixa && ((clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente) || (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)))
						{

							clsDocVenda.TrataIvaCaixa = Convert.ToBoolean(clsEntidade.TrataIvaCaixa);

						}

					}

				}
				else
				{

					//Entidades Externas
					clsDocVenda.Nome = Convert.ToString(clsEntidade.Nome);
					clsDocVenda.Morada = Convert.ToString(clsEntidade.Morada);
					clsDocVenda.Morada2 = Convert.ToString(clsEntidade.Morada2); //BID 551619
					clsDocVenda.Localidade = Convert.ToString(clsEntidade.Localidade);
					clsDocVenda.NumContribuinte = Convert.ToString(clsEntidade.NumContrib);
					clsDocVenda.CodigoPostal = Convert.ToString(clsEntidade.CodPostal);
					clsDocVenda.LocalidadeCodigoPostal = Convert.ToString(clsEntidade.CodPostalLocal);
					clsDocVenda.Distrito = Convert.ToString(clsEntidade.Distrito);
					clsDocVenda.Pais = Convert.ToString(clsEntidade.Pais); //BID: 567634


					//CR.680 - 750 Alfa7
					clsDocVenda.TipoEntidadeFac = clsDocVenda.TipoEntidade; // [BID:537191/540176] Actualmente apenas este tipo é suportado.
					clsDocVenda.EntidadeFac = clsDocVenda.Entidade;
					clsDocVenda.NomeFac = Convert.ToString(clsEntidade.Nome);
					clsDocVenda.MoradaFac = Convert.ToString(clsEntidade.Morada);
					clsDocVenda.Morada2Fac = Convert.ToString(clsEntidade.Morada2); //BID 551619
					clsDocVenda.LocalidadeFac = Convert.ToString(clsEntidade.Localidade);
					clsDocVenda.NumContribuinteFac = Convert.ToString(clsEntidade.NumContrib);
					clsDocVenda.CodigoPostalFac = Convert.ToString(clsEntidade.CodPostal);
					clsDocVenda.LocalidadeCodigoPostalFac = Convert.ToString(clsEntidade.CodPostalLocal);
					clsDocVenda.DistritoFac = Convert.ToString(clsEntidade.Distrito);
					clsDocVenda.PaisFac = Convert.ToString(clsEntidade.Pais); //BID: 567634
					//^CR.680 - 750 Alfa7


					objConta = m_objErpBSO.Base.ContasBancariasTerceiros.DaContaBancariasDefeito(clsDocVenda.TipoEntidade, clsDocVenda.Entidade);

					if (objConta != null)
					{

						clsDocVenda.ContaDomiciliacao = objConta.Conta;

						//BID 551328
					}
					else
					{

						clsDocVenda.ContaDomiciliacao = "";

					}

					objConta = null;

					clsDocVenda.Moeda = m_objErpBSO.Contexto.MoedaEuro;

					//CS.1398
					clsDocVenda.RegimeIva = "0";

					clsDocVenda.EspacoFiscal = (int) FuncoesComuns100.FuncoesBS.Documentos.DevolveIndiceComboEspacoFiscal(Convert.ToString(clsEntidade.TipoMercado), ConstantesPrimavera100.Modulos.Vendas);

					//END CS.1398

					//O responsável pela conbrança corresponde ao vendedor associado ao cliente
					clsDocVenda.Responsavel = Convert.ToString(clsEntidade.Vendedor);


					//** Parâmetros da aplicação
					clsDocVenda.LocalCarga = m_objErpBSO.Vendas.Params.LocalCarga;
					clsDocVenda.LocalDescarga = m_objErpBSO.Vendas.Params.LocalDescarga;
					clsDocVenda.HoraCarga = Strings.FormatDateTime(DateTime.Now, DateFormat.ShortTime);

					//BID 582491
					clsDocVenda.DataCarga = DateTimeHelper.ToString(DateTime.Today);
					clsDocVenda.DataDescarga = DateTimeHelper.ToString(DateTime.Today);
					//Fim 582491

					if (Strings.Len(clsDocVenda.Seccao) == 0)
					{
						clsDocVenda.Seccao = m_objErpBSO.Vendas.Params.Seccao;
					}

				}

				blnMorAlt = false;

				withVar = clsDocVenda.CargaDescarga;

				//Carga
				withVar.MoradaCarga = m_objErpBSO.Contexto.IDMorada;
				withVar.LocalidadeCarga = m_objErpBSO.Contexto.IDLocalidade;
				withVar.CodPostalCarga = m_objErpBSO.Contexto.IDCodPostal;
				withVar.CodPostalLocalidadeCarga = m_objErpBSO.Contexto.IDCodPostalLocal;
				//BID 598738 : estava sempre a sugerir a propriedade [m_objErpBSO.Contexto.IDDistritoCod]
				withVar.DistritoCarga = FuncoesComuns100.FuncoesBS.Documentos.SugereDistritoEmpresa(m_objErpBSO);
				//UPGRADE_WARNING: (6021) Casting 'ErpBS100.EnumLocalizacaoSede' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				switch((StdBE100.StdBETipos.EnumLocalizacaoSede) m_objErpBSO.Contexto.LocalizacaoSede)
				{
					case StdBE100.StdBETipos.EnumLocalizacaoSede.lsAcores : case StdBE100.StdBETipos.EnumLocalizacaoSede.lsMadeira : case StdBE100.StdBETipos.EnumLocalizacaoSede.lsPortugalCont : 
						strPais = "PT";  //BID 584932 (estava : .PaisCarga = "PT") 'PriGlobal: IGNORE 
						break;
					case StdBE100.StdBETipos.EnumLocalizacaoSede.lsEspanha : 
						strPais = "ES";  //BID 584932 (estava : .PaisCarga = "ES") 'PriGlobal: IGNORE 
						break;
					default:
						strPais = "";  //BID 584932 (nova linha) 
						break;
				}

				//BID 584932
				if (Strings.Len(strPais) > 0)
				{

					//BID 586396
					//Set objLista = m_objErpBSO.Consulta("SELECT TOP 1 Pais FROM Paises WHERE ISOA2='" & strPais & "' AND Pais='" & strPais & "'")
					string tempRefParam5 = "SELECT Pais FROM Paises WHERE ISOA2='" + strPais + "' AND Pais='" + strPais + "' UNION ALL SELECT Pais FROM Paises WHERE ISOA2='" + strPais + "' AND Pais<>'" + strPais + "'";
					objLista = m_objErpBSO.Consulta(tempRefParam5); //PriGlobal: IGNORE
					//Fim 586396

					if (objLista != null)
					{
						if (!objLista.Vazia())
						{
							withVar.PaisCarga = objLista.Valor("Pais");
						}
						else
						{
							withVar.PaisCarga = "";
						}
					}

					objLista = null;

				}
				//Fim 584932

				//Descarga/Entrega
				clsDocVenda.UtilizaMoradaAlternativaEntreg = false;
				clsDocVenda.EntidadeEntrega = clsDocVenda.Entidade;
				clsDocVenda.TipoEntidadeEntrega = clsDocVenda.TipoEntidade; //BID 592091

				if (clsDocVenda.TipoEntidade == "C")
				{

					//BID 583950
					if (Strings.Len(clsDocVenda.MoradaAlternativaEntrega) > 0)
					{

						ClsMorada = m_objErpBSO.Base.MoradasAlternativas.Edita(clsDocVenda.TipoEntidade, clsDocVenda.EntidadeEntrega, clsDocVenda.MoradaAlternativaEntrega);

						if (ClsMorada != null)
						{

							//BID 598770
							clsDocVenda.MoradaAlternativaEntrega = ClsMorada.MoradaAlternativa;
							clsDocVenda.UtilizaMoradaAlternativaEntreg = true;
							withVar.MoradaEntrega = ClsMorada.Morada;
							withVar.Morada2Entrega = ClsMorada.Morada2;
							withVar.LocalidadeEntrega = ClsMorada.Localidade;
							withVar.CodPostalEntrega = ClsMorada.CodigoPostal;
							withVar.CodPostalLocalidadeEntrega = ClsMorada.LocalidadeCodigoPostal;
							withVar.DistritoEntrega = ClsMorada.Distrito;
							withVar.PaisEntrega = ClsMorada.Pais;
							blnMorAlt = true;

						}

					}
					else
					{
						//Fim 583950

						//Verificar qual é a morada alternativa por defeito
						ClsMorada = m_objErpBSO.Base.MoradasAlternativas.DaMoradaAlternativaDefeito(clsDocVenda.TipoEntidade, clsDocVenda.Entidade);
						if (ClsMorada != null)
						{

							clsDocVenda.UtilizaMoradaAlternativaEntreg = true;
							withVar.MoradaEntrega = ClsMorada.Morada;
							withVar.Morada2Entrega = ClsMorada.Morada2;
							withVar.LocalidadeEntrega = ClsMorada.Localidade;
							withVar.CodPostalEntrega = ClsMorada.CodigoPostal;
							withVar.CodPostalLocalidadeEntrega = ClsMorada.LocalidadeCodigoPostal;
							withVar.DistritoEntrega = ClsMorada.Distrito;
							withVar.PaisEntrega = ClsMorada.Pais;
							blnMorAlt = true;
						}

					} //BID 583950

				}

				if (!blnMorAlt)
				{

					withVar.MoradaEntrega = clsDocVenda.Morada;
					withVar.Morada2Entrega = clsDocVenda.Morada2;
					withVar.LocalidadeEntrega = clsDocVenda.Localidade;
					withVar.CodPostalEntrega = clsDocVenda.CodigoPostal;
					withVar.CodPostalLocalidadeEntrega = clsDocVenda.LocalidadeCodigoPostal;
					withVar.DistritoEntrega = clsDocVenda.Distrito;
					withVar.PaisEntrega = clsDocVenda.Pais;

				}
				else if (clsDocVenda.EntidadeDescarga == "")
				{ 

					clsDocVenda.EntidadeDescarga = clsDocVenda.Entidade;

				}

				//Inverte, caso seja de natureza contrária
				if (strPagarReceber == "P")
				{

					//BID 593334 : foi adicionado o parâmetro [blnInverteDataHora]
					FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.InvertePropriedadesCargaDescarga(clsDocVenda.CargaDescarga, false);

				}



				result = clsDocVenda;
				clsEntidade = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosCliente", excep.Message);
			}


			return result;
		}

		private VndBE100.VndBEDocumentoVenda PreencheDadosCondPag(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{
			VndBE100.VndBEDocumentoVenda result = null;
			BasBECondPagamento clsCondPag = null;

			try
			{


				if (Strings.Len(clsDocVenda.CondPag) == 0)
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.PreencheDadosCondPag", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9048, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				clsCondPag = m_objErpBSO.Base.CondsPagamento.Edita(clsDocVenda.CondPag);

				//Calcula a data de vencimento de acordo com a condição de pagamento
				System.DateTime tempRefParam = clsDocVenda.DataDoc;
				string tempRefParam2 = clsDocVenda.CondPag;
				int tempRefParam3 = 0;
				string tempRefParam4 = clsDocVenda.TipoEntidade;
				string tempRefParam5 = clsDocVenda.Entidade;
				clsDocVenda.DataVenc = CalculaDataVencimento(tempRefParam, tempRefParam2, tempRefParam3, tempRefParam4, tempRefParam5);

				//Calcular o desconto financeiro
				clsDocVenda.DescFinanceiro = clsCondPag.Desconto;

				result = clsDocVenda;
				clsCondPag = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheDadosCondPag", excep.Message);
			}

			return result;
		}

		private VndBE100.VndBEDocumentoVenda LimpaDadosMoeda(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{

			try
			{

				clsDocVenda.Cambio = 0;
				clsDocVenda.CambioMBase = 0;
				clsDocVenda.CambioMAlt = 0;
				clsDocVenda.MoedaDaUEM = false;


				return clsDocVenda;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LimpaDadosMoeda", excep.Message);
			}
			return null;
		}

		private VndBE100.VndBEDocumentoVenda LimpaDadosCondPag(VndBE100.VndBEDocumentoVenda clsDocVenda)
		{

			try
			{


				clsDocVenda.DescFinanceiro = 0;
				clsDocVenda.DataVenc = DateTime.Today;


				return clsDocVenda;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LimpaDadosCondPag", excep.Message);
			}
			return null;
		}

		public VndBEDocumentoVenda AdicionaLinha(VndBEDocumentoVenda clsDocVenda, string Artigo, double Quantidade, string Armazem, string Localizacao, double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB, double QntVariavelC, double DescEntidade, double DescFinanceiro, int Arredondamento, int ArredondaIva, bool AdicionaArtigoAssociado, bool PrecoIvaIncluido, double PrecoTaxaIva)
		{


			VndBE100.VndBELinhasDocumentoVenda clsLinhasVenda = null;
			VndBE100.VndBESeccao clsSeccao = null;
			double DescontoEntidade = 0;
			string strCodIva = "";
			string strArtigoPai = "";
			string strIdArtigoPai = "";
			StdBE100.StdBECampos objCampos = null;
			BasBETipos.LOGTipoDocumento intTipoDocumento = BasBETipos.LOGTipoDocumento.LOGDocPedidoCotacao;
			dynamic objCondicoes = null; //PcmBEContratoContabilidade 'Epic 406


			try
			{

				if (DescEntidade == 0)
				{
					DescontoEntidade = clsDocVenda.DescEntidade;
				}
				else
				{
					DescontoEntidade = DescEntidade;
				}

				if (PrecoIvaIncluido && (PrecoTaxaIva == 0))
				{

					//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Artigos.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					strCodIva = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "Iva"));
					//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to double. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					PrecoTaxaIva = ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Iva.DaValorAtributo(strCodIva, "Taxa"));

				}

				//Se o artigo tem Pai, então vamos procurar o id do pai para associarmos este artigo _
				//'Apenas procurar se a ultimo linha tem pai. Se não tiver temos de inserir esse pai.
				strIdArtigoPai = "";
				//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Artigos.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
				strArtigoPai = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "ArtigoPai"));
				//BID 574740 (foi adicionado o parâmetro "strArtigoFilho")
				if (Strings.Len(strArtigoPai) > 0)
				{
					strIdArtigoPai = FuncoesComuns100.FuncoesBS.Documentos.DaUltimoIdLinhaPaiEmColeccao(clsDocVenda.Linhas, strArtigoPai, Artigo);
				}

				//** Para cada linha do documento de venda preencher os dados respectivos
				clsLinhasVenda = SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  AdicionaArtigoAssociado,  PrecoIvaIncluido,  PrecoTaxaIva,  strIdArtigoPai);

				//Só necessita dos valores das entidades se o regime de iva do documento não estiver preenchido
				if (Strings.Len(clsDocVenda.Entidade) != 0 && Strings.Len(clsDocVenda.RegimeIva) == 0)
				{

					//** Preencher o regime de Iva
					//BID: 576188 - CS.3185

					string switchVar = clsDocVenda.TipoEntidade;
					if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
					{

						objCampos = m_objErpBSO.Base.Clientes.DaValorAtributos(clsDocVenda.Entidade, "TipoMercado", "RegimeIvaReembolsos");

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
					{ 

						objCampos = m_objErpBSO.Base.OutrosTerceiros.DaValorAtributos(clsDocVenda.Entidade, clsDocVenda.TipoEntidade, "TipoMercado", "RegimeIvaReembolsos");

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.EntidadeExterna)
					{ 

						objCampos = (StdBE100.StdBECampos) m_objErpBSO.CRM.EntidadesExternas.DaValorAtributos(clsDocVenda.Entidade, "TipoMercado");

					}
					//BID: 576188 - CS.3185
				}

				string tempRefParam = clsDocVenda.Seccao;
				clsSeccao = m_objErpBSO.Vendas.Seccoes.Edita(tempRefParam);

				//UPGRADE_WARNING: (1068) m_objErpBSO.Vendas.TabVendas.DaValorAtributo() of type Variant is being forced to LOGTipoDocumento. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
				string tempRefParam2 = clsDocVenda.Tipodoc;
				string tempRefParam3 = "TipoDocumento";
				intTipoDocumento = ReflectionHelper.GetPrimitiveValue<BasBETipos.LOGTipoDocumento>(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam2, tempRefParam3));

				//** Para cada linha da colecção de linhas sugeridas
				foreach (VndBE100.VndBELinhaDocumentoVenda Linha in clsLinhasVenda)
				{

					//If Len(clsDocVenda.Entidade) <> 0 Then
					if (objCampos != null)
					{

						Linha.TipoOperacao = clsDocVenda.TipoOperacao;

						//BID 575606
						if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal() && Strings.Len(clsDocVenda.TipoOperacao) > 0)
						{

							if (String.CompareOrdinal(Linha.TipoLinha, "20") >= 0 && String.CompareOrdinal(Linha.TipoLinha, "29") <= 0)
							{

								Linha.TipoOperacao = "5";

							}
							else
							{

								Linha.TipoOperacao = "1";

							}

						}
						//Fim 575606

						if (Strings.Len(clsDocVenda.RegimeIva) > 0)
						{

							Linha.RegimeIva = clsDocVenda.RegimeIva;
						}
						else
						{

							string tempRefParam4 = "TipoMercado";
							string tempRefParam5 = "RegimeIvaReembolsos";
							Linha.RegimeIva = m_objErpBSO.DSO.Plat.Utils.FStr((int) FuncoesComuns100.FuncoesBS.Documentos.DevolveEspacoFiscalCalculado(m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam4)), (clsDocVenda.TipoEntidade != ConstantesPrimavera100.TiposEntidade.EntidadeExterna) ? m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.GetItem(ref tempRefParam5)) : 0, m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc, clsDocVenda.Serie, "IvaIncluido")), ConstantesPrimavera100.Modulos.Vendas));
						}

						objCampos = null;

						strCodIva = "";

						//Para o mercado intracomunitario por defeito a regra sugerida é o reverse charge
						if (m_objErpBSO.DSO.Plat.Utils.FInt(clsDocVenda.RegimeIva) == ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoIntracomunitario))
						{

							if (clsDocVenda.RegimeIvaReembolsos == ((int) FuncoesComuns100.clsBSEditoresRegimeIVA.EnuRegimesIva.ReverseCharge) && clsDocVenda.EspacoFiscal == ((int) FuncoesComuns100.clsBSEditoresRegimeIVA.GCPEspacoFiscalCombo.GCPEspacoFiscalIntracomunitario))
							{

								// Sugere IVA Normal
								strCodIva = m_objErpBSO.Base.Params.CodigoIvaIntracom;

							}
							else if (clsDocVenda.RegimeIvaReembolsos == ((int) FuncoesComuns100.clsBSEditoresRegimeIVA.EnuRegimesIva.ReverseCharge) && clsDocVenda.EspacoFiscal == ((int) FuncoesComuns100.clsBSEditoresRegimeIVA.GCPEspacoFiscalCombo.GCPEspacoFiscalNormal))
							{ 

								// Sugere IVA Reverse
								strCodIva = m_objErpBSO.Base.Params.IvaReverseCharge;

							}

							Linha.IvaRegraCalculo = 1;

						}
						else if (m_objErpBSO.DSO.Plat.Utils.FInt(clsDocVenda.RegimeIva) == ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva))
						{ 

							// Sugere IVA Isento
							strCodIva = m_objErpBSO.Base.Params.CodigoIvaIsento;

							Linha.IvaRegraCalculo = 0;

						}
						else if (m_objErpBSO.DSO.Plat.Utils.FInt(clsDocVenda.RegimeIva) == ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoExterno))
						{ 

							// Sugere IVA Mercado Externo
							strCodIva = m_objErpBSO.Base.Params.CodigoIvaExterno;

						}

						if (Strings.Len(strCodIva) > 0)
						{

							FuncoesComuns100.FuncoesBS.Documentos.DaTaxaIvaLocalOperacao(ref strCodIva, clsDocVenda.LocalOperacao);

							objCampos = m_objErpBSO.Base.Iva.DaValorAtributos(strCodIva, "Taxa", "TaxaRecargo");

							Linha.CodIva = strCodIva;
							string tempRefParam6 = "Taxa";
							Linha.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam6).Valor);
							string tempRefParam7 = "TaxaRecargo";
							Linha.TaxaRecargo = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam7).Valor);

							objCampos = null;
						}


						clsSeccao = null;
						objCampos = null;

					}

					//** Preencher a data nas linhas
					//BID 527917 (foi adicionado o "if Linha.DataStock = 0 Then")
					if (Linha.DataStock == DateTime.FromOADate(0))
					{
						Linha.DataStock = clsDocVenda.DataDoc;
					}

					if (intTipoDocumento == BasBETipos.LOGTipoDocumento.LOGDocEncomenda)
					{

						Linha.DataEntrega = DateTime.Parse(DateTimeHelper.ToString(clsDocVenda.DataDoc.AddDays(ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(Linha.Artigo, "PrazoEntrega")))) + " " + DateTimeHelper.Time.ToString("HH:mm:SS"));
					}

					//Epic 406
					if (Strings.Len(clsDocVenda.IdContrato) > 0)
					{

						Linha.IdContrato = clsDocVenda.IdContrato;

						objCondicoes = Contratos.EditaCondicoesCBL(Linha.IdContrato);

						if (objCondicoes != null)
						{

							Linha.CCustoCBL = objCondicoes.CCustoCBL;
							Linha.FuncionalCBL = objCondicoes.FuncionalCBL;
							Linha.AnaliticaCBL = objCondicoes.AnaliticaCBL;
							Linha.Vendedor = Contratos.DevolveValorContrato(Linha.IdContrato, "Vendedor"); //PriGlobal: IGNORE

							objCondicoes = null;

						}

					}

					clsDocVenda.Linhas.Insere(Linha);

				}


				return clsDocVenda;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.AdicionaLinha", excep.Message);
			}
			return null;
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade,  double DescFinanceiro,  int Arredondamento,  int ArredondaIva,  bool AdicionaArtigoAssociado,  bool PrecoIvaIncluido)
		{
			double tempRefParam182 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  DescFinanceiro,  Arredondamento,  ArredondaIva,  AdicionaArtigoAssociado,  PrecoIvaIncluido,  tempRefParam182);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade,  double DescFinanceiro,  int Arredondamento,  int ArredondaIva,  bool AdicionaArtigoAssociado)
		{
			bool tempRefParam183 = false;
			double tempRefParam184 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  DescFinanceiro,  Arredondamento,  ArredondaIva,  AdicionaArtigoAssociado,  tempRefParam183,  tempRefParam184);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade,  double DescFinanceiro,  int Arredondamento,  int ArredondaIva)
		{
			bool tempRefParam185 = true;
			bool tempRefParam186 = false;
			double tempRefParam187 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  DescFinanceiro,  Arredondamento,  ArredondaIva,  tempRefParam185,  tempRefParam186,  tempRefParam187);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade,  double DescFinanceiro,  int Arredondamento)
		{
			int tempRefParam188 = 0;
			bool tempRefParam189 = true;
			bool tempRefParam190 = false;
			double tempRefParam191 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  DescFinanceiro,  Arredondamento,  tempRefParam188,  tempRefParam189,  tempRefParam190,  tempRefParam191);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade,  double DescFinanceiro)
		{
			int tempRefParam192 = 0;
			int tempRefParam193 = 0;
			bool tempRefParam194 = true;
			bool tempRefParam195 = false;
			double tempRefParam196 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  DescFinanceiro,  tempRefParam192,  tempRefParam193,  tempRefParam194,  tempRefParam195,  tempRefParam196);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  double DescEntidade)
		{
			double tempRefParam197 = 0;
			int tempRefParam198 = 0;
			int tempRefParam199 = 0;
			bool tempRefParam200 = true;
			bool tempRefParam201 = false;
			double tempRefParam202 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  DescEntidade,  tempRefParam197,  tempRefParam198,  tempRefParam199,  tempRefParam200,  tempRefParam201,  tempRefParam202);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC)
		{
			double tempRefParam203 = 0;
			double tempRefParam204 = 0;
			int tempRefParam205 = 0;
			int tempRefParam206 = 0;
			bool tempRefParam207 = true;
			bool tempRefParam208 = false;
			double tempRefParam209 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  tempRefParam203,  tempRefParam204,  tempRefParam205,  tempRefParam206,  tempRefParam207,  tempRefParam208,  tempRefParam209);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB)
		{
			double tempRefParam210 = 0;
			double tempRefParam211 = 0;
			double tempRefParam212 = 0;
			int tempRefParam213 = 0;
			int tempRefParam214 = 0;
			bool tempRefParam215 = true;
			bool tempRefParam216 = false;
			double tempRefParam217 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  tempRefParam210,  tempRefParam211,  tempRefParam212,  tempRefParam213,  tempRefParam214,  tempRefParam215,  tempRefParam216,  tempRefParam217);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA)
		{
			double tempRefParam218 = 0;
			double tempRefParam219 = 0;
			double tempRefParam220 = 0;
			double tempRefParam221 = 0;
			int tempRefParam222 = 0;
			int tempRefParam223 = 0;
			bool tempRefParam224 = true;
			bool tempRefParam225 = false;
			double tempRefParam226 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  tempRefParam218,  tempRefParam219,  tempRefParam220,  tempRefParam221,  tempRefParam222,  tempRefParam223,  tempRefParam224,  tempRefParam225,  tempRefParam226);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote)
		{
			double tempRefParam227 = 0;
			double tempRefParam228 = 0;
			double tempRefParam229 = 0;
			double tempRefParam230 = 0;
			double tempRefParam231 = 0;
			int tempRefParam232 = 0;
			int tempRefParam233 = 0;
			bool tempRefParam234 = true;
			bool tempRefParam235 = false;
			double tempRefParam236 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  tempRefParam227,  tempRefParam228,  tempRefParam229,  tempRefParam230,  tempRefParam231,  tempRefParam232,  tempRefParam233,  tempRefParam234,  tempRefParam235,  tempRefParam236);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto)
		{
			string tempRefParam237 = "";
			double tempRefParam238 = 0;
			double tempRefParam239 = 0;
			double tempRefParam240 = 0;
			double tempRefParam241 = 0;
			double tempRefParam242 = 0;
			int tempRefParam243 = 0;
			int tempRefParam244 = 0;
			bool tempRefParam245 = true;
			bool tempRefParam246 = false;
			double tempRefParam247 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  tempRefParam237,  tempRefParam238,  tempRefParam239,  tempRefParam240,  tempRefParam241,  tempRefParam242,  tempRefParam243,  tempRefParam244,  tempRefParam245,  tempRefParam246,  tempRefParam247);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario)
		{
			double tempRefParam248 = -1;
			string tempRefParam249 = "";
			double tempRefParam250 = 0;
			double tempRefParam251 = 0;
			double tempRefParam252 = 0;
			double tempRefParam253 = 0;
			double tempRefParam254 = 0;
			int tempRefParam255 = 0;
			int tempRefParam256 = 0;
			bool tempRefParam257 = true;
			bool tempRefParam258 = false;
			double tempRefParam259 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  tempRefParam248,  tempRefParam249,  tempRefParam250,  tempRefParam251,  tempRefParam252,  tempRefParam253,  tempRefParam254,  tempRefParam255,  tempRefParam256,  tempRefParam257,  tempRefParam258,  tempRefParam259);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao)
		{
			double tempRefParam260 = -1;
			double tempRefParam261 = -1;
			string tempRefParam262 = "";
			double tempRefParam263 = 0;
			double tempRefParam264 = 0;
			double tempRefParam265 = 0;
			double tempRefParam266 = 0;
			double tempRefParam267 = 0;
			int tempRefParam268 = 0;
			int tempRefParam269 = 0;
			bool tempRefParam270 = true;
			bool tempRefParam271 = false;
			double tempRefParam272 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  tempRefParam260,  tempRefParam261,  tempRefParam262,  tempRefParam263,  tempRefParam264,  tempRefParam265,  tempRefParam266,  tempRefParam267,  tempRefParam268,  tempRefParam269,  tempRefParam270,  tempRefParam271,  tempRefParam272);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem)
		{
			string tempRefParam273 = "";
			double tempRefParam274 = -1;
			double tempRefParam275 = -1;
			string tempRefParam276 = "";
			double tempRefParam277 = 0;
			double tempRefParam278 = 0;
			double tempRefParam279 = 0;
			double tempRefParam280 = 0;
			double tempRefParam281 = 0;
			int tempRefParam282 = 0;
			int tempRefParam283 = 0;
			bool tempRefParam284 = true;
			bool tempRefParam285 = false;
			double tempRefParam286 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  Armazem,  tempRefParam273,  tempRefParam274,  tempRefParam275,  tempRefParam276,  tempRefParam277,  tempRefParam278,  tempRefParam279,  tempRefParam280,  tempRefParam281,  tempRefParam282,  tempRefParam283,  tempRefParam284,  tempRefParam285,  tempRefParam286);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade)
		{
			string tempRefParam287 = "";
			string tempRefParam288 = "";
			double tempRefParam289 = -1;
			double tempRefParam290 = -1;
			string tempRefParam291 = "";
			double tempRefParam292 = 0;
			double tempRefParam293 = 0;
			double tempRefParam294 = 0;
			double tempRefParam295 = 0;
			double tempRefParam296 = 0;
			int tempRefParam297 = 0;
			int tempRefParam298 = 0;
			bool tempRefParam299 = true;
			bool tempRefParam300 = false;
			double tempRefParam301 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  Quantidade,  tempRefParam287,  tempRefParam288,  tempRefParam289,  tempRefParam290,  tempRefParam291,  tempRefParam292,  tempRefParam293,  tempRefParam294,  tempRefParam295,  tempRefParam296,  tempRefParam297,  tempRefParam298,  tempRefParam299,  tempRefParam300,  tempRefParam301);
		}

		public VndBEDocumentoVenda AdicionaLinha( VndBEDocumentoVenda clsDocVenda,  string Artigo)
		{
			double tempRefParam302 = 1;
			string tempRefParam303 = "";
			string tempRefParam304 = "";
			double tempRefParam305 = -1;
			double tempRefParam306 = -1;
			string tempRefParam307 = "";
			double tempRefParam308 = 0;
			double tempRefParam309 = 0;
			double tempRefParam310 = 0;
			double tempRefParam311 = 0;
			double tempRefParam312 = 0;
			int tempRefParam313 = 0;
			int tempRefParam314 = 0;
			bool tempRefParam315 = true;
			bool tempRefParam316 = false;
			double tempRefParam317 = 0;
			return AdicionaLinha( clsDocVenda,  Artigo,  tempRefParam302,  tempRefParam303,  tempRefParam304,  tempRefParam305,  tempRefParam306,  tempRefParam307,  tempRefParam308,  tempRefParam309,  tempRefParam310,  tempRefParam311,  tempRefParam312,  tempRefParam313,  tempRefParam314,  tempRefParam315,  tempRefParam316,  tempRefParam317);
		}

		//BID: 573153
		private void ActualizaLinhaPai(VndBE100.VndBEDocumentoVenda objDocVenda, string strIdLinhaPai, double dblQuantidade)
		{
			int i = 0;
			double dblQtdPai = 0;


			try
			{
				i = 0;
				int tempForVar = objDocVenda.Linhas.NumItens;
				for (i = 1; i <= tempForVar; i++)
				{
					if (objDocVenda.Linhas.GetEdita(i).IdLinha == strIdLinhaPai)
					{
						dblQtdPai = 0;
						int tempForVar2 = objDocVenda.Linhas.NumItens;
						for (int j = i; j <= tempForVar2; j++)
						{
							if (objDocVenda.Linhas.GetEdita(j).IdLinhaPai == strIdLinhaPai)
							{
								dblQtdPai += objDocVenda.Linhas.GetEdita(j).Quantidade;
							}
						}
						objDocVenda.Linhas.GetEdita(i).Quantidade = dblQtdPai + dblQuantidade;
						break;
					}
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaLinhaPai", excep.Message); //PriGlobal: IGNORE
			}

		}
		//^BID: 573153

		private string TrataLinhaPai(VndBE100.VndBEDocumentoVenda objDocVenda, VndBE100.VndBELinhasDocumentoVenda objLinhasDocVenda, BasBEArtigo ObjArtigo, ref string strIdLinhaPai, ref double dblQuantidade)
		{
			string result = "";
			BasBEArtigo objArtigoPai = null;
			VndBE100.VndBELinhaDocumentoVenda objLinhaDocVenda = null;

			try
			{

				if (ObjArtigo.TrataDimensao == ConstantesPrimavera100.Artigos.TratamentoDimensoesFilho && Strings.Len(strIdLinhaPai) == 0)
				{
					if (Strings.Len(ObjArtigo.ArtigoPai) > 0)
					{
						objArtigoPai = FuncoesComuns100.FuncoesBS.Documentos.CarregaDadosArtigoSugereLinha(ObjArtigo.ArtigoPai);
						if (objArtigoPai == null)
						{
							string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(10108, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam2 = new dynamic[]{ObjArtigo.ArtigoPai};
							StdErros.StdRaiseErro(3000, "_VNDBSVendas.SugereArtigoLinhas", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2));
						}
						else
						{
							objLinhaDocVenda = SugereLinha(objDocVenda, objArtigoPai, ref dblQuantidade);

							strIdLinhaPai = objLinhaDocVenda.IdLinha;
							objLinhasDocVenda.Insere(objLinhaDocVenda);
						}
						objArtigoPai = null;
					}
				}
				else if (ObjArtigo.TrataDimensao == ConstantesPrimavera100.Artigos.TratamentoDimensoesFilho && Strings.Len(strIdLinhaPai) != 0)
				{ 
					foreach (VndBE100.VndBELinhaDocumentoVenda objLinhaDocVenda2 in objLinhasDocVenda)
					{
						objLinhaDocVenda = objLinhaDocVenda2;
						if (objLinhaDocVenda.IdLinha == strIdLinhaPai)
						{
							objLinhaDocVenda.Quantidade += dblQuantidade;
							break;
						}
						objLinhaDocVenda = null;
					}

				}

				result = strIdLinhaPai;

				objArtigoPai = null;
				objLinhaDocVenda = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.TrataLinhaPai", excep.Message); //PriGlobal: IGNORE
			}
			return result;
		}

		//Nova entrada numa venda. Devolve uma ou mais linhas (artigos simples/compostos).
		//Sugere uma ou mais linhas (artigos simples/compostos)
		public VndBELinhasDocumentoVenda SugereArtigoLinhas(VndBEDocumentoVenda clsDocVenda, string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  bool AdicionaArtigoAssociado,  bool PrecoIvaIncluido,  double PrecoTaxaIva,  string IdLinhaPai)
		{
			VndBELinhasDocumentoVenda result = null;
			VndBE100.VndBELinhasDocumentoVenda clsLinhasVendas = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda = null;
			BasBEArtigo clsArtigo = null;
			BasBEArtigoComponentes objBEComponentes = null;
			BasBEArtigo clsArtigoAssoc = null;
			double dblQuantidade = 0;

			try
			{

				clsLinhasVendas = new VndBE100.VndBELinhasDocumentoVenda();
				ClsLinhaVenda = new VndBE100.VndBELinhaDocumentoVenda();
				string tempRefParam = "LinhasDoc";
				ClsLinhaVenda.CamposUtil = m_objErpBSO.DSO.Plat.CamposUtilizador.DaCamposUtil(tempRefParam); //PriGlobal: IGNORE

				if (Strings.Len(Artigo) != 0)
				{

					// Edita o objecto artigo
					clsArtigo = FuncoesComuns100.FuncoesBS.Documentos.CarregaDadosArtigoSugereLinha(Artigo);

					if (clsArtigo != null)
					{


						switch(clsArtigo.Classe)
						{
							case 0 : case 2 :  //Diferente de Conjunto de Artigos, ou seja, Simples ou Composto 
								 
								//Se o artigo é filho de dimensão e não tem referência ao pai (IdLinhaPai)_ 
								//então devemos inserie a linha do pai e de seguida a do filho 
								 
								//BID 572663 (estava "clsLinhasVendas" em vez de "clsDocVenda.Linhas") 
								IdLinhaPai = TrataLinhaPai(clsDocVenda, clsLinhasVendas, clsArtigo, ref IdLinhaPai, ref Quantidade); 
								ClsLinhaVenda = SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, PrecoIvaIncluido, PrecoTaxaIva, IdLinhaPai); 
								clsLinhasVendas.Insere(ClsLinhaVenda); 
								if (Strings.Len(IdLinhaPai) > 0)
								{
									ActualizaLinhaPai(clsDocVenda, IdLinhaPai, Quantidade);
								}  //BID: 573153 e 576016 
								 
								break;
							case 1 :  //Conjunto de artigos 
								// Edita os componentes deste artigo 
								objBEComponentes = m_objErpBSO.Base.ArtigosComponentes.ListaArtigosComponentes(Artigo); 
								 
								//Insere a linha de descrição correspondente ao artigo composto 
								InsereLinhaComposto(ClsLinhaVenda, clsLinhasVendas, clsArtigo.Descricao); 
								 
								//Para cada elemento do conjunto dos artigos 
								foreach (BasBEArtigoComponente clsComponente in objBEComponentes)
								{
									clsArtigo = FuncoesComuns100.FuncoesBS.Documentos.CarregaDadosArtigoSugereLinha(clsComponente.Componente);

									dblQuantidade = clsComponente.Quantidade * Quantidade;

									Armazem = clsComponente.Armazem;
									Localizacao = clsComponente.Localizacao;

									IdLinhaPai = "";

									//BID 574740 (foi adicionado o parâmetro "strArtigoFilho")
									if (Strings.Len(clsArtigo.ArtigoPai) > 0)
									{
										IdLinhaPai = FuncoesComuns100.FuncoesBS.Documentos.DaUltimoIdLinhaPaiEmColeccao(clsLinhasVendas, clsArtigo.ArtigoPai, clsArtigo.Artigo);
									}

									IdLinhaPai = TrataLinhaPai(clsDocVenda, clsLinhasVendas, clsArtigo, ref IdLinhaPai, ref dblQuantidade);

									//BID 591272 : foram adicionados os parâmetros "PrecoIvaIncluido" e "PrecoTaxaIva"
									ClsLinhaVenda = SugereLinha(clsDocVenda, clsArtigo, ref dblQuantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, PrecoIvaIncluido, PrecoTaxaIva, IdLinhaPai);
									clsLinhasVendas.Insere(ClsLinhaVenda);

									ClsLinhaVenda = null;
									//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
									//clsComponente = null;
								} 
								 
								objBEComponentes = null; 
								break;
						}

						//Artigo associado
						if (AdicionaArtigoAssociado)
						{
							if (Strings.Len(clsArtigo.ArtigoAssociado) != 0)
							{
								clsArtigoAssoc = FuncoesComuns100.FuncoesBS.Documentos.CarregaDadosArtigoSugereLinha(clsArtigo.ArtigoAssociado);

								//BID 540156
								if (clsArtigoAssoc.Classe == 1)
								{ //Conjunto de Artigos
									// Edita os componentes do artigo associado
									objBEComponentes = m_objErpBSO.Base.ArtigosComponentes.ListaArtigosComponentes(clsArtigoAssoc.Artigo);

									//Insere a linha de descrição correspondente ao artigo composto
									ClsLinhaVenda = null;
									ClsLinhaVenda = new VndBE100.VndBELinhaDocumentoVenda();
									InsereLinhaComposto(ClsLinhaVenda, clsLinhasVendas, clsArtigoAssoc.Descricao);

									//Para cada elemento do conjunto dos artigos
									foreach (BasBEArtigoComponente clsComponente in objBEComponentes)
									{
										clsArtigo = FuncoesComuns100.FuncoesBS.Documentos.CarregaDadosArtigoSugereLinha(clsComponente.Componente);
										dblQuantidade = clsComponente.Quantidade * Quantidade;
										Armazem = clsComponente.Armazem;
										Localizacao = clsComponente.Localizacao;
										IdLinhaPai = "";

										//BID 574740 (foi adicionado o parâmetro "strArtigoFilho")
										if (Strings.Len(clsArtigo.ArtigoPai) > 0)
										{
											IdLinhaPai = FuncoesComuns100.FuncoesBS.Documentos.DaUltimoIdLinhaPaiEmColeccao(clsLinhasVendas, clsArtigo.ArtigoPai, clsArtigo.Artigo);
										}

										IdLinhaPai = TrataLinhaPai(clsDocVenda, clsLinhasVendas, clsArtigo, ref IdLinhaPai, ref dblQuantidade);

										//BID 591272 : foram adicionados os parâmetros "PrecoIvaIncluido" e "PrecoTaxaIva"
										ClsLinhaVenda = SugereLinha(clsDocVenda, clsArtigo, ref dblQuantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, PrecoIvaIncluido, PrecoTaxaIva, IdLinhaPai);
										clsLinhasVendas.Insere(ClsLinhaVenda);
										ClsLinhaVenda = null;
										//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
										//clsComponente = null;
									}

									objBEComponentes = null;
								}
								else
								{
									//Fim 540156
									Quantidade = ClsLinhaVenda.Quantidade;

									//BID 543870
									//Set ClsLinhaVenda = SugereLinha(clsDocVenda, clsArtigoAssoc, Quantidade, Armazem, Localizacao, 0, 0)
									ClsLinhaVenda = SugereLinha(clsDocVenda, clsArtigoAssoc, ref Quantidade, Armazem, Localizacao);
									//Fim 543870
									clsLinhasVendas.Insere(ClsLinhaVenda);
								} //BID 540156

								clsArtigoAssoc = null;
							}
						}
					}
					else
					{
						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.SugereArtigoLinhas", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3875, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
					}
				}

				result = clsLinhasVendas;

				clsArtigo = null;
				ClsLinhaVenda = null;
				clsLinhasVendas = null;
			}
			catch (System.Exception excep)
			{



				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.SugereArtigoLinhas", excep.Message);
			}

			return result;
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  bool AdicionaArtigoAssociado,  bool PrecoIvaIncluido,  double PrecoTaxaIva)
		{
			string tempRefParam318 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  AdicionaArtigoAssociado,  PrecoIvaIncluido,  PrecoTaxaIva,  tempRefParam318);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  bool AdicionaArtigoAssociado,  bool PrecoIvaIncluido)
		{
			double tempRefParam319 = 0;
			string tempRefParam320 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  AdicionaArtigoAssociado,  PrecoIvaIncluido,  tempRefParam319,  tempRefParam320);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC,  bool AdicionaArtigoAssociado)
		{
			bool tempRefParam321 = false;
			double tempRefParam322 = 0;
			string tempRefParam323 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  AdicionaArtigoAssociado,  tempRefParam321,  tempRefParam322,  tempRefParam323);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB,  double QntVariavelC)
		{
			bool tempRefParam324 = true;
			bool tempRefParam325 = false;
			double tempRefParam326 = 0;
			string tempRefParam327 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  QntVariavelC,  tempRefParam324,  tempRefParam325,  tempRefParam326,  tempRefParam327);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA,  double QntVariavelB)
		{
			double tempRefParam328 = 0;
			bool tempRefParam329 = true;
			bool tempRefParam330 = false;
			double tempRefParam331 = 0;
			string tempRefParam332 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  QntVariavelB,  tempRefParam328,  tempRefParam329,  tempRefParam330,  tempRefParam331,  tempRefParam332);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote,  double QntVariavelA)
		{
			double tempRefParam333 = 0;
			double tempRefParam334 = 0;
			bool tempRefParam335 = true;
			bool tempRefParam336 = false;
			double tempRefParam337 = 0;
			string tempRefParam338 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  QntVariavelA,  tempRefParam333,  tempRefParam334,  tempRefParam335,  tempRefParam336,  tempRefParam337,  tempRefParam338);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto,  string Lote)
		{
			double tempRefParam339 = 0;
			double tempRefParam340 = 0;
			double tempRefParam341 = 0;
			bool tempRefParam342 = true;
			bool tempRefParam343 = false;
			double tempRefParam344 = 0;
			string tempRefParam345 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  Lote,  tempRefParam339,  tempRefParam340,  tempRefParam341,  tempRefParam342,  tempRefParam343,  tempRefParam344,  tempRefParam345);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario,  double Desconto)
		{
			string tempRefParam346 = "";
			double tempRefParam347 = 0;
			double tempRefParam348 = 0;
			double tempRefParam349 = 0;
			bool tempRefParam350 = true;
			bool tempRefParam351 = false;
			double tempRefParam352 = 0;
			string tempRefParam353 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  Desconto,  tempRefParam346,  tempRefParam347,  tempRefParam348,  tempRefParam349,  tempRefParam350,  tempRefParam351,  tempRefParam352,  tempRefParam353);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao,  double PrecoUnitario)
		{
			double tempRefParam354 = -1;
			string tempRefParam355 = "";
			double tempRefParam356 = 0;
			double tempRefParam357 = 0;
			double tempRefParam358 = 0;
			bool tempRefParam359 = true;
			bool tempRefParam360 = false;
			double tempRefParam361 = 0;
			string tempRefParam362 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  PrecoUnitario,  tempRefParam354,  tempRefParam355,  tempRefParam356,  tempRefParam357,  tempRefParam358,  tempRefParam359,  tempRefParam360,  tempRefParam361,  tempRefParam362);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem,  string Localizacao)
		{
			double tempRefParam363 = -1;
			double tempRefParam364 = -1;
			string tempRefParam365 = "";
			double tempRefParam366 = 0;
			double tempRefParam367 = 0;
			double tempRefParam368 = 0;
			bool tempRefParam369 = true;
			bool tempRefParam370 = false;
			double tempRefParam371 = 0;
			string tempRefParam372 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  Localizacao,  tempRefParam363,  tempRefParam364,  tempRefParam365,  tempRefParam366,  tempRefParam367,  tempRefParam368,  tempRefParam369,  tempRefParam370,  tempRefParam371,  tempRefParam372);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade,  string Armazem)
		{
			string tempRefParam373 = "";
			double tempRefParam374 = -1;
			double tempRefParam375 = -1;
			string tempRefParam376 = "";
			double tempRefParam377 = 0;
			double tempRefParam378 = 0;
			double tempRefParam379 = 0;
			bool tempRefParam380 = true;
			bool tempRefParam381 = false;
			double tempRefParam382 = 0;
			string tempRefParam383 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  Armazem,  tempRefParam373,  tempRefParam374,  tempRefParam375,  tempRefParam376,  tempRefParam377,  tempRefParam378,  tempRefParam379,  tempRefParam380,  tempRefParam381,  tempRefParam382,  tempRefParam383);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo,  double Quantidade)
		{
			string tempRefParam384 = "";
			string tempRefParam385 = "";
			double tempRefParam386 = -1;
			double tempRefParam387 = -1;
			string tempRefParam388 = "";
			double tempRefParam389 = 0;
			double tempRefParam390 = 0;
			double tempRefParam391 = 0;
			bool tempRefParam392 = true;
			bool tempRefParam393 = false;
			double tempRefParam394 = 0;
			string tempRefParam395 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  Quantidade,  tempRefParam384,  tempRefParam385,  tempRefParam386,  tempRefParam387,  tempRefParam388,  tempRefParam389,  tempRefParam390,  tempRefParam391,  tempRefParam392,  tempRefParam393,  tempRefParam394,  tempRefParam395);
		}

		public VndBELinhasDocumentoVenda SugereArtigoLinhas( VndBEDocumentoVenda clsDocVenda,  string Artigo)
		{
			double tempRefParam396 = 1;
			string tempRefParam397 = "";
			string tempRefParam398 = "";
			double tempRefParam399 = -1;
			double tempRefParam400 = -1;
			string tempRefParam401 = "";
			double tempRefParam402 = 0;
			double tempRefParam403 = 0;
			double tempRefParam404 = 0;
			bool tempRefParam405 = true;
			bool tempRefParam406 = false;
			double tempRefParam407 = 0;
			string tempRefParam408 = "";
			return SugereArtigoLinhas( clsDocVenda,  Artigo,  tempRefParam396,  tempRefParam397,  tempRefParam398,  tempRefParam399,  tempRefParam400,  tempRefParam401,  tempRefParam402,  tempRefParam403,  tempRefParam404,  tempRefParam405,  tempRefParam406,  tempRefParam407,  tempRefParam408);
		}

		private void InsereLinhaComposto(VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda, VndBE100.VndBELinhasDocumentoVenda clsLinhasVenda, string Descricao)
		{

			try
			{

				//Inserir a respectiva descrição do artigo composoto ao documento de venda.
				ClsLinhaVenda.TipoLinha = ConstantesPrimavera100.Documentos.TipoLinComentario;
				ClsLinhaVenda.Descricao = Descricao;
				ClsLinhaVenda.Quantidade = 0;

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(ClsLinhaVenda.CamposUtil, DaDefCamposUtilLinhas());

				clsLinhasVenda.Insere(ClsLinhaVenda);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.InsereLinhaComposto", excep.Message);
			}

		}


		//BID 525270 - DA
		private void SugerePrecoDescontoUserExit(System.DateTime DataDoc, string Moeda, string Cliente, string Artigo, string Contrato, string Unidade, double Quantidade, double FactorConversao, double PrecoUnit, double PrecoSugerido, bool IvaIncluido, double TaxaIva, double DescontoSugerido, double DescontoSugerido1, double DescontoSugerido2, double DescontoSugerido3, bool SugereSemRegras = true, bool SugerePreco = true, bool SugereDesc = true, bool SoComEscaloes = false)
		{

			int lngExisteUserExit = 0;
			dynamic objBS = null;


			//-- Possibilidade de extensibilidade neste método

			try
			{

				// 1. Verificar se esta opção está activa (Config.INI).
				//
				//   [BSUserExits]
				//   BSVendas=1

				string tempRefParam = "BSUserExits";
				string tempRefParam2 = "BSVendas";
				int tempRefParam3 = 0;
				StdBE100.StdBETipos.TipoIni tempRefParam4 = StdBE100.StdBETipos.TipoIni.inGlobalSistema;
				lngExisteUserExit = m_objErpBSO.DSO.Plat.IniFiles.IniLeLong(ref tempRefParam, tempRefParam2, tempRefParam3, tempRefParam4);

				if (lngExisteUserExit > 0)
				{

					// 2. Instanciar o componente
					objBS = UpgradeHelpers.SupportHelper.Support.CreateObject("VndBS100Ex.GcpBSEx", "");
					objBS.ErpBSO = m_objErpBSO;

					// 3. Invocar o método do componente
					objBS.Vendas.SugerePrecoDesconto(DataDoc, Moeda, Cliente, Artigo, Unidade, Quantidade, FactorConversao, PrecoUnit, PrecoSugerido, IvaIncluido, TaxaIva, DescontoSugerido, DescontoSugerido1, DescontoSugerido2, DescontoSugerido3, SugereSemRegras, SugerePreco, SugereDesc, SoComEscaloes);

					// Libertar a instanciação do objecto
					objBS = null;

					//Nova versão da user exit. Para não quebrar a antiga, é uma nova EX
					SugerePrecoDescontoUserExit2(DataDoc, Moeda, Cliente, Artigo, Contrato, Unidade, Quantidade, FactorConversao, PrecoUnit, PrecoSugerido, IvaIncluido, TaxaIva, DescontoSugerido, DescontoSugerido1, DescontoSugerido2, DescontoSugerido3, SugereSemRegras, SugerePreco, SugereDesc, SoComEscaloes);

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				throw new System.Exception(Information.Err().Number.ToString() + ", _BSVendasEx.SugerePrecoDesconto, " + Information.Err().Number.ToString() + " - " + excep.Message);
			}

		}

		private void SugerePrecoDescontoUserExit2(System.DateTime DataDoc, string Moeda, string Cliente, string Artigo, string Contrato, string Unidade, double Quantidade, double FactorConversao, double PrecoUnit, double PrecoSugerido, bool IvaIncluido, double TaxaIva, double DescontoSugerido, double DescontoSugerido1, double DescontoSugerido2, double DescontoSugerido3, bool SugereSemRegras = true, bool SugerePreco = true, bool SugereDesc = true, bool SoComEscaloes = false)
		{
			dynamic objBS = null;

			// 2. Instanciar o componente
			try
			{

				objBS = UpgradeHelpers.SupportHelper.Support.CreateObject("VndBS100Ex2.GcpBSEx2", "");
				objBS.ErpBSO = m_objErpBSO;

				// 3. Invocar o método do componente
				objBS.Vendas.SugerePrecoDesconto(DataDoc, Moeda, Cliente, Artigo, Contrato, Unidade, Quantidade, FactorConversao, PrecoUnit, PrecoSugerido, IvaIncluido, TaxaIva, DescontoSugerido, DescontoSugerido1, DescontoSugerido2, DescontoSugerido3, SugereSemRegras, SugerePreco, SugereDesc, SoComEscaloes);

				// Libertar a instanciação do objecto
				objBS = null;
			}
			catch
			{
			}


		}

		//MA: Tornar este método público para que possa ser usado na interface
		private void PreencheHistoricoResiduos(VndBE100.VndBELinhaDocumentoVenda ObjLinha, string strLocalOperacao = "")
		{
			BasBEArtigosResiduos objArtigosResiduos = null;
			BasBELinhaHistoricoResiduo objLinhaHistResiduo = null;
			StdBE100.StdBECampos objCamposResiduo = null; //CR.715

			try
			{ //CR.715

				objArtigosResiduos = new BasBEArtigosResiduos();

				objArtigosResiduos = m_objErpBSO.Base.ArtigosResiduos.ListaArtigosResiduosEX(ObjLinha.Artigo, ConstantesPrimavera100.Modulos.Vendas, strLocalOperacao);

				if (objArtigosResiduos != null)
				{
					foreach (BasBEArtigoResiduo ObjArtigoResiduo in objArtigosResiduos)
					{
						objLinhaHistResiduo = new BasBELinhaHistoricoResiduo();
						objCamposResiduo = m_objErpBSO.Base.Residuos.DaValorAtributos(ObjArtigoResiduo.Residuo, "Ecovalor", "Categoria"); //CR.715

						bool tempRefParam = true;
						objLinhaHistResiduo.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);
						objLinhaHistResiduo.IdLinha = ObjLinha.IdLinha;
						objLinhaHistResiduo.Residuo = ObjArtigoResiduo.Residuo;
						objLinhaHistResiduo.Quantidade = ObjArtigoResiduo.Quantidade;
						objLinhaHistResiduo.Modulo = ConstantesPrimavera100.Modulos.Vendas;
						objLinhaHistResiduo.IVA = ObjLinha.CodIva;
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to double. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						objLinhaHistResiduo.TaxaIva = ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Iva.DaValorAtributo(objLinhaHistResiduo.IVA, "Taxa"));
						//CR.715
						//.Ecovalor = m_objErpBSO.DSO.Plat.Utils.FDbl(objCamposResiduo("Ecovalor"))
						objLinhaHistResiduo.Ecovalor = m_objErpBSO.DSO.Plat.Utils.FDbl(ObjArtigoResiduo.Ecovalor); //BID: 552792
						objLinhaHistResiduo.Tipo = ObjArtigoResiduo.TipoResiduo;
						string tempRefParam2 = "Categoria";
						objLinhaHistResiduo.Categoria = m_objErpBSO.DSO.Plat.Utils.FStr(objCamposResiduo.GetItem(ref tempRefParam2)); //BID 550304 (estava "m_objErpBSO.DSO.Plat.Utils.FDbl(...)") 'PriGlobal: IGNORE
						objLinhaHistResiduo.Unidade = ObjArtigoResiduo.Unidade;
						//^CR.715
						objLinhaHistResiduo.QtdResiduo = ObjArtigoResiduo.QtdResiduo; //cr.806

						ObjLinha.LinhasHistoricoResiduo.Insere(objLinhaHistResiduo);
						objCamposResiduo = null; //CR.715
					}
				}
			}
			catch (System.Exception excep)
			{

				//CR.715
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSCompras.PreencheHistoricoResiduos", excep.Message); //PriGlobal: IGNORE
			}

		}


		private string DevolvePrimeiroArmazem(ref string Artigo)
		{
			string strQuery = "";
			ADORecordSetHelper rs = null;
			string Armazem = "";

			try
			{

				strQuery = "SELECT TOP 1 Armazem FROM  dbo.INV_ListaStockDataArm ('@1@', DEFAULT, DEFAULT, DEFAULT)";
				dynamic[] tempRefParam = new dynamic[]{Artigo};
				strQuery = m_objErpBSO.DSO.Plat.Strings.Formata(strQuery, tempRefParam);
				rs = ADORecordSetHelper.Open(strQuery, m_objErpBSO.DSO.BDAPL, "");

				if (!(rs.BOF && rs.EOF))
				{

					Armazem = "" + Convert.ToString(rs["Armazem"]);

				}
				else
				{

					Armazem = "";

				}

				rs.Close();
				rs = null;

				return Armazem;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VndBSCompras.DevolvePrimeiroArmazem", excep.Message); //PriGlobal: IGNORE
			}
			return "";
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB, double QntVariavelC, bool PrecoIvaIncluido, double PrecoTaxaIva, string IdLinhaPai)
		{

			VndBE100.VndBELinhaDocumentoVenda result = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda = null;
			StdBE100.StdBECampos objCampos = null;
			StdBE100.StdBECampos objCamposEntidade = null;

			double PrecoSugerir = 0;
			double DescontoSugerir = 0;
			double DescontoSugerir1 = 0;
			double DescontoSugerir2 = 0;
			double DescontoSugerir3 = 0;
			dynamic varAux = null;
			bool IvaIncluido = false;
			string strTaxaIva = "";
			bool blnEntIsentaIEC = false;
			string strTipoMercado = "";
			BasBETiposGcp.EnumRegimesIncidenciaIva intRegraIncidenciaIva = BasBETiposGcp.EnumRegimesIncidenciaIva.Normal;
			double dblBaseCalcInc = 0;
			string strMovStock = "";
			string strContrato = "";
			string strEstadoOrigem = "";
			string strEstadoDestino = "";
			InvBE100.InvBETipos.EnumTipoConfigEstados intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

			try
			{

				ClsLinhaVenda = new VndBE100.VndBELinhaDocumentoVenda();
				string tempRefParam = "LinhasDoc";
				ClsLinhaVenda.CamposUtil = m_objErpBSO.DSO.Plat.CamposUtilizador.DaCamposUtil(tempRefParam); //PriGlobal: IGNORE

				IvaIncluido = (m_objErpBSO.DSO.Plat.Utils.FInt(clsDocVenda.RegimeIva) == 1);

				bool tempRefParam2 = true;
				ClsLinhaVenda.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam2);

				//Se o cliente está preenchido edita-o
				if (Strings.Len(clsDocVenda.Entidade) > 0)
				{


					string switchVar = clsDocVenda.TipoEntidade;
					if (switchVar == ConstantesPrimavera100.TiposEntidade.Cliente)
					{

						objCamposEntidade = m_objErpBSO.Base.Clientes.DaValorAtributos(clsDocVenda.Entidade, "TipoMercado", "SujeitoRecargo", "Vendedor", "IsentoIEC", "UtilizaIdioma", "Idioma");

						if (objCamposEntidade != null)
						{

							string tempRefParam3 = "IsentoIEC";
							blnEntIsentaIEC = m_objErpBSO.DSO.Plat.Utils.FBool(objCamposEntidade.GetItem(ref tempRefParam3)); //CS.242_7.50_Alfa8 - IEC 'PriGlobal: IGNORE
						}

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
					{ 

						objCamposEntidade = m_objErpBSO.Base.OutrosTerceiros.DaValorAtributos(clsDocVenda.Entidade, clsDocVenda.TipoEntidade, "TipoMercado", "SujeitoRecargo", "Vendedor");
						blnEntIsentaIEC = true;

					}
					else if (switchVar == ConstantesPrimavera100.TiposEntidade.EntidadeExterna)
					{ 

						objCamposEntidade = (StdBE100.StdBECampos) m_objErpBSO.CRM.EntidadesExternas.DaValorAtributos(clsDocVenda.Entidade, "TipoMercado");
						blnEntIsentaIEC = true;

					}
				}

				ClsLinhaVenda.Artigo = clsArtigo.Artigo;
				ClsLinhaVenda.Quantidade = Quantidade;

				//Descrição do artigo
				ClsLinhaVenda.Descricao = clsArtigo.Descricao;

				//Se existir, colocar a descrição na linguagem do cliente.
				if (objCamposEntidade != null)
				{

					if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente)
					{

						string tempRefParam4 = "UtilizaIdioma";
						if (m_objErpBSO.DSO.Plat.Utils.FBool(objCamposEntidade.GetItem(ref tempRefParam4).Valor))
						{ //PriGlobal: IGNORE

							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.ArtigosIdiomas.DaValorAtributo() of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							string tempRefParam5 = "Idioma";
							varAux = ReflectionHelper.GetPrimitiveValue(m_objErpBSO.Base.ArtigosIdiomas.DaValorAtributo(clsArtigo.Artigo, m_objErpBSO.DSO.Plat.Utils.FStr(objCamposEntidade.GetItem(ref tempRefParam5).Valor), "Descricao"));

							if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(varAux)) > 0)
							{

								ClsLinhaVenda.Descricao = m_objErpBSO.DSO.Plat.Utils.FStr(varAux);
							}
						}
					}
				}

				ClsLinhaVenda.TipoLinha = m_objErpBSO.Base.TiposArtigo.DaTipoLinha(clsArtigo.TipoArtigo);

				//Preenche o armazém sugestão
				if (Strings.Len(Armazem) == 0)
				{

					//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Series.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					ClsLinhaVenda.Armazem = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc, clsDocVenda.Serie, "ArmazemSugestao"));

					if (Strings.Len(ClsLinhaVenda.Armazem) == 0)
					{

						ClsLinhaVenda.Armazem = clsArtigo.ArmazemSugestao;

						if (Strings.Len(clsArtigo.ArmazemSugestao) == 0)
						{

							string tempRefParam6 = clsArtigo.Artigo;
							ClsLinhaVenda.Armazem = DevolvePrimeiroArmazem(ref tempRefParam6);

						}
					}
				}
				else
				{

					ClsLinhaVenda.Armazem = Armazem;
				}

				//Preenche a localizacao sugestão
				if (Strings.Len(Localizacao) == 0)
				{

					//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Series.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					ClsLinhaVenda.Localizacao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.Tipodoc, clsDocVenda.Serie, "LocalSugestao"));

					if (Strings.Len(ClsLinhaVenda.Localizacao) == 0)
					{

						if (Strings.Len(clsArtigo.LocalizacaoSugestao) > 0)
						{

							ClsLinhaVenda.Localizacao = clsArtigo.LocalizacaoSugestao;
						}
						else
						{

							ClsLinhaVenda.Localizacao = ClsLinhaVenda.Armazem;
						}
					}
					//Fim 541504
				}
				else
				{

					ClsLinhaVenda.Localizacao = Localizacao;
				}

				//Preenche o lote de sugestão
				if (Strings.Len(Lote) == 0)
				{

					ClsLinhaVenda.Lote = Convert.ToString(m_objErpBSO.Inventario.ArtigosLotes.DaLoteSaida(ClsLinhaVenda.Artigo));
				}
				else
				{

					ClsLinhaVenda.Lote = Lote;
				}

				// Unidade a sugerir
				ClsLinhaVenda.Unidade = clsArtigo.UnidadeVenda;

				if (ClsLinhaVenda.Unidade == "")
				{

					ClsLinhaVenda.Unidade = clsArtigo.UnidadeBase;
				}

				string tempRefParam7 = clsArtigo.Artigo;
				string tempRefParam8 = ClsLinhaVenda.Unidade;
				string tempRefParam9 = clsArtigo.UnidadeBase;
				ClsLinhaVenda.FactorConv = m_objErpBSO.Base.Artigos.FactorConvUnBaseUnDest(ref tempRefParam7, ref tempRefParam8, ref tempRefParam9);

				// Avisos Stock
				ClsLinhaVenda.MovStock = clsArtigo.MovStock;

				if (clsArtigo.MovStock == "S")
				{

					ClsLinhaVenda.StockActual = Convert.ToDouble(m_objErpBSO.Inventario.Stocks.DaStockArtigoBE(ClsLinhaVenda.Artigo, ClsLinhaVenda.DataStock, ClsLinhaVenda.Armazem, ClsLinhaVenda.Localizacao, ClsLinhaVenda.Lote));

					//Estados do inventário
					if (Quantidade >= 0)
					{

						intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

					}
					else
					{

						intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovNegativos;

					}

					m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), clsDocVenda.Tipodoc, intTipoMov, strEstadoOrigem, strEstadoDestino);
					ClsLinhaVenda.INV_EstadoOrigem = strEstadoOrigem;
					ClsLinhaVenda.INV_EstadoDestino = strEstadoDestino;


				}
				else
				{

					ClsLinhaVenda.StockActual = 0;
				}

				ClsLinhaVenda.DataStock = m_objErpBSO.DSO.Plat.Utils.FData(DateTimeHelper.ToString(clsDocVenda.DataDoc) + " " + DateTimeHelper.Time.ToString("HH:mm:SS"));

				//Fórmula associada ao artigo
				ClsLinhaVenda.Formula = clsArtigo.FormulaSaidas;
				ClsLinhaVenda.VariavelA = QntVariavelA;
				ClsLinhaVenda.VariavelB = QntVariavelB;
				ClsLinhaVenda.VariavelC = QntVariavelC;

				if (Strings.Len(ClsLinhaVenda.Formula) != 0)
				{

					ClsLinhaVenda.QuantFormula = m_objErpBSO.Base.Formulas.DaQuantidadeFormula(ClsLinhaVenda.Formula, ClsLinhaVenda.VariavelA, ClsLinhaVenda.VariavelB, ClsLinhaVenda.VariavelC);
				}
				else
				{

					ClsLinhaVenda.QuantFormula = 1;
				}

				ClsLinhaVenda.Quantidade = ClsLinhaVenda.QuantFormula * ClsLinhaVenda.Quantidade;

				if (clsArtigo.TrataDimensao == ConstantesPrimavera100.Artigos.TratamentoDimensoesPai)
				{

					ClsLinhaVenda.TipoLinha = ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo;
				}
				else
				{

					if (clsArtigo.TrataDimensao == ConstantesPrimavera100.Artigos.TratamentoDimensoesFilho)
					{

						ClsLinhaVenda.IdLinhaPai = IdLinhaPai;
					}
					else
					{

						ClsLinhaVenda.IdLinhaPai = "";
					}

					//Taxa e código do I.V.A.
					ClsLinhaVenda.CodIva = clsArtigo.IVA;
					ClsLinhaVenda.PercIncidenciaIVA = (float) clsArtigo.PercIncidenciaIVA;
					ClsLinhaVenda.TaxaIva = 0;
					ClsLinhaVenda.TaxaRecargo = 0;
					ClsLinhaVenda.PercIvaDedutivel = clsArtigo.PercIvaDedutivel;
					ClsLinhaVenda.IvaRegraCalculo = 0;

					//Se o cliente for do mercado nacional, senão não deduz iva.
					if (objCamposEntidade != null)
					{

						strTipoMercado = "0";

						if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente || clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
						{

							string tempRefParam10 = "TipoMercado";
							strTipoMercado = m_objErpBSO.DSO.Plat.Utils.FStr(objCamposEntidade.GetItem(ref tempRefParam10)); //PriGlobal: IGNORE
						}

						if ((clsDocVenda.RegimeIva == "0" || clsDocVenda.RegimeIva == "1" || clsDocVenda.RegimeIva == "2") || (strTipoMercado == "0" && Strings.Len(clsDocVenda.RegimeIva) == 0))
						{

							strTaxaIva = ClsLinhaVenda.CodIva;

							//Taxa e código do I.V.A. por local de operação
							FuncoesComuns100.FuncoesBS.Documentos.DaTaxaIvaLocalOperacao(ref strTaxaIva, clsDocVenda.LocalOperacao);

							if (clsDocVenda.RegimeIva == "2")
							{
								ClsLinhaVenda.CodIva = m_objErpBSO.Base.Params.CodigoIvaIsento;
							}
							else
							{
								ClsLinhaVenda.CodIva = strTaxaIva;
							}

							objCampos = m_objErpBSO.Base.Iva.DaValorAtributos(ClsLinhaVenda.CodIva, "Taxa", "TaxaRecargo");
							string tempRefParam11 = "Taxa";
							ClsLinhaVenda.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam11).Valor);

							if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente || clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
							{ //BID: 576188 - CS.3185

								string tempRefParam12 = "SujeitoRecargo";
								if (m_objErpBSO.DSO.Plat.Utils.FBool(objCamposEntidade.GetItem(ref tempRefParam12)) && FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoLinhaParaRecargo(ClsLinhaVenda.TipoLinha))
								{ //PriGlobal: IGNORE

									string tempRefParam13 = "TaxaRecargo";
									ClsLinhaVenda.TaxaRecargo = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam13).Valor);
								}
							}

							objCampos = null;
						}
						else
						{

							//Para o mercado intracomunitario por defeito a regra sugerida é o reverse charge
							//Obtem a taxa de iva para o local de operação
							//Se é externo deve ler a taxa de iva do mercado externo
							string tempRefParam14 = "TipoMercado";
							if (m_objErpBSO.DSO.Plat.Utils.FInt(objCamposEntidade.GetItem(ref tempRefParam14)) == 2)
							{ //PriGlobal: IGNORE

								strTaxaIva = m_objErpBSO.Base.Params.CodigoIvaExterno;
								ClsLinhaVenda.IvaRegraCalculo = 0;
							}
							else
							{

								strTaxaIva = m_objErpBSO.Base.Params.CodigoIvaIntracom;
								ClsLinhaVenda.IvaRegraCalculo = 1;
							}

							FuncoesComuns100.FuncoesBS.Documentos.DaTaxaIvaLocalOperacao(ref strTaxaIva, clsDocVenda.LocalOperacao);

							objCampos = m_objErpBSO.Base.Iva.DaValorAtributos(strTaxaIva, "Taxa", "TaxaRecargo");

							ClsLinhaVenda.CodIva = strTaxaIva;
							string tempRefParam15 = "Taxa";
							ClsLinhaVenda.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam15).Valor);
							string tempRefParam16 = "TaxaRecargo";
							ClsLinhaVenda.TaxaRecargo = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam16).Valor);

						}
					}
					else
					{

						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to float. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						ClsLinhaVenda.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(m_objErpBSO.Base.Iva.DaValorAtributo(ClsLinhaVenda.CodIva, "Taxa"));
					}

					//Retira o contrato 1º das linhas e se não tiver vai ao cabeçalho
					strContrato = "";
					strContrato = ClsLinhaVenda.IdContrato;

					if (Strings.Len(strContrato) == 0)
					{

						strContrato = clsDocVenda.IdContrato;

					}

					if (Strings.Len(strContrato) != 0)
					{

						strContrato = Convert.ToString(m_objErpBSO.Contratos.Contratos.DaValorAtributoID(strContrato, "Codigo"));

					}

					if (!clsDocVenda.CalculoManual)
					{

						//Sugere o desconto e/ou o preço do artigo de acordo com as regras de descontos de preços
						System.DateTime tempRefParam17 = DateTime.Today;
						string tempRefParam18 = clsDocVenda.Moeda;
						string tempRefParam19 = clsDocVenda.Entidade;
						string tempRefParam20 = ClsLinhaVenda.Artigo;
						string tempRefParam21 = ClsLinhaVenda.Unidade;
						double tempRefParam22 = ClsLinhaVenda.FactorConv;
						double tempRefParam23 = ClsLinhaVenda.TaxaIva;
						SugerePrecoDesconto( tempRefParam17,  tempRefParam18,  tempRefParam19,  tempRefParam20,  strContrato,  tempRefParam21,  Quantidade,  tempRefParam22,  PrecoUnitario,  PrecoSugerir,  IvaIncluido,  tempRefParam23,  DescontoSugerir,  DescontoSugerir1,  DescontoSugerir2,  DescontoSugerir3);
					}

					//Preço
					if (PrecoUnitario == -1)
					{

						if (PrecoSugerir == -1)
						{

							ClsLinhaVenda.PrecUnit = 0;
						}
						else
						{

							ClsLinhaVenda.PrecUnit = PrecoSugerir;
						}
					}
					else
					{

						if (IvaIncluido || PrecoIvaIncluido)
						{

							ClsLinhaVenda.PrecUnit = m_objErpBSO.Base.Artigos.ConvertePreco(PrecoUnitario, PrecoIvaIncluido, PrecoTaxaIva, IvaIncluido, ClsLinhaVenda.TaxaIva, ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Moedas.DaValorAtributo(clsDocVenda.Moeda, "DecPrecUnit")));
						}
						else
						{

							ClsLinhaVenda.PrecUnit = PrecoUnitario;
						}
					}

					if (Desconto == -1)
					{

						if (DescontoSugerir == -1)
						{

							ClsLinhaVenda.Desconto1 = 0;
							ClsLinhaVenda.Desconto2 = 0;
							ClsLinhaVenda.Desconto3 = 0;
						}
						else
						{

							ClsLinhaVenda.Desconto1 = (float) DescontoSugerir1;
							ClsLinhaVenda.Desconto2 = (float) DescontoSugerir2;
							ClsLinhaVenda.Desconto3 = (float) DescontoSugerir3;
						}
					}
					else
					{

						ClsLinhaVenda.Desconto1 = (float) Desconto;
						ClsLinhaVenda.Desconto2 = 0;
						ClsLinhaVenda.Desconto3 = 0;
					}

					if (Strings.Len(clsDocVenda.RegimeIva) > 0)
					{

						if (clsArtigo.SujeitoEcotaxa && StringsHelper.ToDoubleSafe(clsDocVenda.RegimeIva) != 3 && StringsHelper.ToDoubleSafe(clsDocVenda.RegimeIva) != 4)
						{
							//US 7426
							ClsLinhaVenda.Ecotaxa = STDPriAPIDivisas.TransfMBaseMEsp(FuncoesComuns100.FuncoesBS.Documentos.DaTotalEcovalor(m_objErpBSO, clsArtigo.Artigo, ConstantesPrimavera100.Modulos.Vendas, clsDocVenda.LocalOperacao), clsDocVenda.CambioMBase, clsDocVenda.Cambio, m_objErpBSO.Base.Params.CasasDecimaisEcovalor);

							ClsLinhaVenda.CodIvaEcotaxa = ClsLinhaVenda.CodIva;
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to float. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							ClsLinhaVenda.TaxaIvaEcotaxa = ReflectionHelper.GetPrimitiveValue<float>(m_objErpBSO.Base.Iva.DaValorAtributo(ClsLinhaVenda.CodIvaEcotaxa, "Taxa"));
							PreencheHistoricoResiduos(ClsLinhaVenda, clsDocVenda.LocalOperacao);
						}
					}

					if (clsArtigo.SujeitoIEC && (!blnEntIsentaIEC))
					{

						//BID 598521
						//Casas decimais do IEC fixadas em 4 no GCPEditor através da veriável global NUMCAS_DEC_IEC
						ClsLinhaVenda.ValorIEC = STDPriAPIDivisas.TransfMBaseMEsp(m_objErpBSO.Base.HistoricoIEC.CalculaValorIEC(clsArtigo.Artigo, ClsLinhaVenda.Unidade, blnEntIsentaIEC), clsDocVenda.CambioMBase, clsDocVenda.Cambio, 4);
						ClsLinhaVenda.CodIvaIEC = ClsLinhaVenda.CodIva;
						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to float. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						ClsLinhaVenda.TaxaIvaIEC = ReflectionHelper.GetPrimitiveValue<float>(m_objErpBSO.Base.Iva.DaValorAtributo(ClsLinhaVenda.CodIvaIEC, "Taxa"));
					}


					//Regimes especiais de IVA e IPC
					string tempRefParam24 = clsDocVenda.Tipodoc;
					string tempRefParam25 = "TipoDocSTK";
					strMovStock = m_objErpBSO.DSO.Plat.Utils.FStr(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam24, tempRefParam25));

					//Sugerir os valores por omissão para a linha
					//Calcula os valores
					dblBaseCalcInc = FuncoesComuns100.FuncoesBS.Documentos.CalculaBaseCalculoIncidenciaIVA(ConstantesPrimavera100.Modulos.Vendas, ClsLinhaVenda.CodIva, ClsLinhaVenda.Artigo, ClsLinhaVenda.Quantidade, strMovStock, clsDocVenda.Moeda, clsDocVenda.Cambio, clsDocVenda.CambioMAlt, clsDocVenda.CambioMBase, ClsLinhaVenda.Armazem, ClsLinhaVenda.Lote, ClsLinhaVenda.Localizacao, ref intRegraIncidenciaIva);
					ClsLinhaVenda.BaseCalculoIncidencia = dblBaseCalcInc;
					ClsLinhaVenda.RegraCalculoIncidencia = intRegraIncidenciaIva;

					//Actualizar o preço líquido nas linhas
					if (Strings.Len(clsDocVenda.RegimeIva) > 0)
					{
						CalculaValoresLinhas(ClsLinhaVenda, clsDocVenda.DescEntidade, clsDocVenda.DescFinanceiro, Convert.ToInt32(Double.Parse(clsDocVenda.RegimeIva)), clsDocVenda.Arredondamento, clsDocVenda.ArredondamentoIva, ClsLinhaVenda.TaxaRecargo != 0, clsDocVenda.Versao, strMovStock);
					}

					if (clsArtigo.SujeitoIEC)
					{

						FuncoesComuns100.FuncoesBS.Documentos.PreencheHistoricoIEC(ClsLinhaVenda, ConstantesPrimavera100.Modulos.Vendas);
					}

					//Intrastat
					ClsLinhaVenda.IntrastatMassaLiq = clsArtigo.IntrastatPesoLiquido;
					ClsLinhaVenda.IntrastatCodigoPautal = clsArtigo.IntrastatCodigoPautal;

					// Comissão do Vendedor
					if (clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente || clsDocVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)
					{ //BID: 576188 - CS.3185

						if (objCamposEntidade != null)
						{

							string tempRefParam26 = "Vendedor";
							ClsLinhaVenda.Vendedor = m_objErpBSO.DSO.Plat.Utils.FStr(objCamposEntidade.GetItem(ref tempRefParam26)); //PriGlobal: IGNORE
							System.DateTime tempRefParam27 = DateTime.Today;
							string tempRefParam28 = clsDocVenda.Moeda;
							string tempRefParam29 = ClsLinhaVenda.Unidade;
							string tempRefParam30 = ClsLinhaVenda.Vendedor;
							string tempRefParam31 = ClsLinhaVenda.Artigo;
							string tempRefParam32 = clsDocVenda.Entidade;
							double tempRefParam33 = ClsLinhaVenda.PrecUnit;
							double tempRefParam34 = ClsLinhaVenda.Quantidade;
							ClsLinhaVenda.Comissao = m_objErpBSO.Vendas.ComissoesVendedor.SugereComissaoVendedor(tempRefParam27, tempRefParam28, tempRefParam29, tempRefParam30, tempRefParam31, tempRefParam32, tempRefParam33, tempRefParam34);
						}
					}

					// Preço de Custo Médio
					ClsLinhaVenda.PCM = Convert.ToDouble(m_objErpBSO.Inventario.Custeio.DaCusto(ClsLinhaVenda.Artigo, ClsLinhaVenda.Armazem, null, ClsLinhaVenda.Lote, ClsLinhaVenda.DataStock, ClsLinhaVenda.Quantidade));

					ClsLinhaVenda.SujeitoRetencao = clsArtigo.SujeitoRetencao;
					//BID 16861
					ClsLinhaVenda.CodigoBarras = clsArtigo.CodBarras;
				}

				// Imposto de selo
				if (m_objErpBSO.Base.Params.SujeitoImpostoSelo)
				{

					ClsLinhaVenda.DadosImpostoSelo.Ano = m_objErpBSO.DSO.Plat.Utils.FInt(clsDocVenda.DataDoc.Year);
					ClsLinhaVenda.DadosImpostoSelo.Selo = m_objErpBSO.DSO.Plat.Utils.FStr(clsArtigo.Selo);
				}

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(ClsLinhaVenda.CamposUtil, DaDefCamposUtilLinhas());

				objCamposEntidade = null;
				result = ClsLinhaVenda;
				ClsLinhaVenda = null;
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.SugereLinha", excep.Message);
			}

			return result;
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB, double QntVariavelC, bool PrecoIvaIncluido, double PrecoTaxaIva)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, PrecoIvaIncluido, PrecoTaxaIva, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB, double QntVariavelC, bool PrecoIvaIncluido)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, PrecoIvaIncluido, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB, double QntVariavelC)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, QntVariavelC, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA, double QntVariavelB)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, QntVariavelB, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote, double QntVariavelA)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, QntVariavelA, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto, string Lote)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, Lote, 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario, double Desconto)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, Desconto, "", 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao, ref double PrecoUnitario)
		{
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref PrecoUnitario, -1, "", 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem, string Localizacao)
		{
			double tempRefParam409 = -1;
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, Localizacao, ref tempRefParam409, -1, "", 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade, string Armazem)
		{
			double tempRefParam410 = -1;
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, Armazem, "", ref tempRefParam410, -1, "", 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo, ref double Quantidade)
		{
			double tempRefParam411 = -1;
			return SugereLinha(clsDocVenda, clsArtigo, ref Quantidade, "", "", ref tempRefParam411, -1, "", 0, 0, 0, false, 0, "");
		}

		private VndBE100.VndBELinhaDocumentoVenda SugereLinha(VndBE100.VndBEDocumentoVenda clsDocVenda, BasBEArtigo clsArtigo)
		{
			double tempRefParam412 = 1;
			double tempRefParam413 = -1;
			return SugereLinha(clsDocVenda, clsArtigo, ref tempRefParam412, "", "", ref tempRefParam413, -1, "", 0, 0, 0, false, 0, "");
		}

		//Calcula a data de vencimento do documento de acordo com a data referida.
		public System.DateTime CalculaDataVencimento( System.DateTime DataDoc,  string CondPag,  int Dias,  string TipoEntidade,  string Entidade)
		{

			try
			{


				return m_objErpBSO.Base.CondsPagamento.CalculaDataVencimento(DataDoc, CondPag, Dias, TipoEntidade, Entidade);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.CalculaDataVenc", excep.Message);
			}
			return DateTime.FromOADate(0);
		}

		public System.DateTime CalculaDataVencimento( System.DateTime DataDoc,  string CondPag,  int Dias,  string TipoEntidade)
		{
			string tempParam414 = "";
			return CalculaDataVencimento( DataDoc,  CondPag,  Dias,  TipoEntidade,  tempParam414);
		}

		public System.DateTime CalculaDataVencimento( System.DateTime DataDoc,  string CondPag,  int Dias)
		{
			string tempParam415 = "";
			string tempParam416 = "";
			return CalculaDataVencimento( DataDoc,  CondPag,  Dias,  tempParam415,  tempParam416);
		}

		public System.DateTime CalculaDataVencimento( System.DateTime DataDoc,  string CondPag)
		{
			int tempParam417 = 0;
			string tempParam418 = "";
			string tempParam419 = "";
			return CalculaDataVencimento( DataDoc,  CondPag,  tempParam417,  tempParam418,  tempParam419);
		}

		public dynamic CalculaRetencoes(VndBEDocumentoVenda objVenda)
		{
			return FuncoesComuns100.FuncoesBS.Documentos.CalculaRetencoesVndComp(objVenda, ConstantesPrimavera100.Modulos.Vendas);
		}

		public VndBEDocumentoVenda AdicionaLinhaTransformada( VndBEDocumentoVenda clsDocVenda,  string TipoDocEnc,  int NumDocEnc,  int NumLinEnc,  string FilialEnc,  string strSerieEnc,  double QuantSatisf)
		{
			VndBEDocumentoVenda result = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaTrans = null;
			VndBE100.VndBEDocumentoVenda clsDocOriginal = null;
			//BID 564203
			StdBE100.StdBELista objLista = null;
			string strSQL = "";
			//Fim 564203
			//BID 578230
			BasBEArtigo clsArtigo = null;
			string strIdLinhaPai = "";
			//Fim 578230
			VndBE100.VndBELinhasDocumentoVenda objLinhasNovas = null;

			try
			{

				if (Strings.Len(FilialEnc) == 0)
				{
					FilialEnc = clsDocVenda.Filial;
				}
				if (strSerieEnc == "")
				{
					strSerieEnc = clsDocVenda.Serie;
				}

				//BID 596719
				if (m_objErpBSO.Vendas.Documentos.DocumentoAnulado(FilialEnc, TipoDocEnc, strSerieEnc, NumDocEnc))
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "IGcpBSVendas_AdicionaLinhaTransformada", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16009, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));

				}

				//Edita a venda para ir buscar os dados necessários
				clsDocOriginal = new VndBE100.VndBEDocumentoVenda();
				clsDocOriginal.Tipodoc = TipoDocEnc;
				clsDocOriginal.Serie = strSerieEnc;
				clsDocOriginal.NumDoc = NumDocEnc;
				clsDocOriginal.Filial = FilialEnc;

				strSQL = "SELECT LD.*, A.TratamentoDim, A.TratamentoSeries, A.CodBarras, A.SujeitoEcotaxa, IDLinhasDocOrigem = NULL,";

				//Quando foi definida a quant, então não é necessário ir à Status saber quanto falta
				if (QuantSatisf == -1)
				{

					strSQL = strSQL + " LDS.QuantTrans, LDS.QuantCopiada,LDS.EstadoTrans, LDS.Fechado, LDS.QuantReserv ";
				}
				else
				{
					strSQL = strSQL + " QuantTrans = 0, QuantCopiada = 0,EstadoTrans ='P', Fechado = 0, QuantReserv = 0 ";

				}

				strSQL = strSQL + " FROM LinhasDoc LD ";
				strSQL = strSQL + " INNER JOIN CabecDoc CD ON CD.Id = LD.IdCabecDoc ";

				//Quando foi definida a quant, então não é necessário ir à Status saber quanto falta
				if (QuantSatisf == -1)
				{

					strSQL = strSQL + " LEFT OUTER JOIN LinhasDocStatus LDS On LDS.IDLinhasDoc=LD.ID  ";

				}

				strSQL = strSQL + " LEFT OUTER JOIN Artigo A ON LD.Artigo = A.Artigo  ";
				strSQL = strSQL + " WHERE CD.TipoDoc = '@1@' AND CD.Serie = '@2@' AND CD.Filial = '@3@' AND CD.NumDoc = @4@ AND LD.NumLinha = @5@ ";
				dynamic[] tempRefParam = new dynamic[]{TipoDocEnc, strSerieEnc, FilialEnc, NumDocEnc, NumLinEnc};
				strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

				//Edita a linha da venda a satisfazer
				m_objErpBSO.DSO.Vendas.Documentos.PreencheObjVendaLinhas( clsDocOriginal,  strSQL);
				ClsLinhaTrans = clsDocOriginal.Linhas.GetEdita(1);

				ClsLinhaVenda = new VndBE100.VndBELinhaDocumentoVenda();

				ClsLinhaVenda.IdLinha = ClsLinhaTrans.IdLinha;

				//Preencher os campos relativos à filial,... que fez a venda
				ClsLinhaVenda.IDLinhaOriginal = ClsLinhaTrans.IdLinha;

				string tempRefParam2 = clsDocVenda.Tipodoc;
				PreencheLinhaTransformada(ClsLinhaVenda, ClsLinhaTrans, ref TipoDocEnc, ref tempRefParam2);
				//atribuição da data do documento à linha
				ClsLinhaVenda.DataStock = clsDocVenda.DataDoc.AddDays(DateTimeHelper.Time.ToOADate()); //BID 576590 (foi adicionado "+ time")

				//Se não está especificada a quantidade é sugerida a quantidade da linha de encomenda
				//If QuantSatisf = -1 Then .Quantidade = ClsLinhaEnc.Quantidade Else .Quantidade = QuantSatisf
				if (QuantSatisf == -1)
				{
					if (ClsLinhaTrans.Quantidade > ClsLinhaTrans.QuantSatisfeita)
					{
						ClsLinhaVenda.Quantidade = ClsLinhaTrans.Quantidade - ClsLinhaTrans.QuantSatisfeita;
					}
					else
					{
						ClsLinhaVenda.Quantidade = 0;
					}
				}
				else
				{
					ClsLinhaVenda.Quantidade = QuantSatisf;
				}


				//BID 564203
				if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(TipoDocEnc, m_ColLigaCamposUtilTranf))
				{

					objLista = (StdBE100.StdBELista) m_ColLigaCamposUtilTranf[TipoDocEnc];

				}
				else
				{

					strSQL = "SELECT * FROM LigacaoCamposUtil WHERE Operacao='2' AND TabelaOrigem='LinhasDoc' AND TabelaDestino='LinhasDoc' AND (DocumentoOrigem='' OR DocumentoOrigem='" + TipoDocEnc + "') ORDER BY DocumentoOrigem";
					objLista = m_objErpBSO.Consulta(strSQL);
					m_ColLigaCamposUtilTranf.Add(TipoDocEnc, objLista);

				}

				if (!objLista.Vazia())
				{

					objLista.Inicio();

					while (!objLista.NoFim())
					{

						if (objLista.Valor("CampoOrigem").ToUpper().StartsWith("CDU_"))
						{
							//UPGRADE_WARNING: (1068) ClsLinhaTrans.CamposUtil().Valor of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							string tempRefParam3 = objLista.Valor("CampoOrigem");
							string tempRefParam4 = objLista.Valor("CampoDestino");
							ClsLinhaVenda.CamposUtil.GetItem(ref tempRefParam4).Valor = ReflectionHelper.GetPrimitiveValue(ClsLinhaTrans.CamposUtil.GetItem(ref tempRefParam3).Valor);
						}
						else
						{
							//UPGRADE_WARNING: (1068) CallByName() of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							string tempRefParam5 = objLista.Valor("CampoDestino");
							ClsLinhaVenda.CamposUtil.GetItem(ref tempRefParam5).Valor = ReflectionHelper.GetPrimitiveValue(Interaction.CallByName(ClsLinhaTrans, objLista.Valor("CampoOrigem"), CallType.Get));
						}

						objLista.Seguinte();

					}

				}

				objLista = null;
				//Fim 564203

				//BID 578230
				if (Strings.Len(ClsLinhaVenda.Artigo) > 0)
				{

					if (ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(ClsLinhaVenda.Artigo, "TratamentoDim")) == ConstantesPrimavera100.Artigos.TratamentoDimensoesFilho)
					{

						clsArtigo = m_objErpBSO.Base.Artigos.Consulta(ClsLinhaVenda.Artigo);

						double tempRefParam6 = ClsLinhaVenda.Quantidade;
						strIdLinhaPai = TrataLinhaPai(clsDocVenda, clsDocVenda.Linhas, clsArtigo, ref strIdLinhaPai, ref tempRefParam6);
						if (Strings.Len(strIdLinhaPai) > 0)
						{
							ClsLinhaVenda.IdLinhaPai = strIdLinhaPai;
						}

						clsArtigo = null;

					}

				}
				//Fim 578230

				objLinhasNovas = new VndBE100.VndBELinhasDocumentoVenda();
				objLinhasNovas.Insere(ClsLinhaVenda);
				ClsLinhaVenda = null;

				DesdobraLinhasReservadas(objLinhasNovas);

				foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaVenda2 in objLinhasNovas)
				{
					ClsLinhaVenda = ClsLinhaVenda2;

					bool tempRefParam7 = true;
					ClsLinhaVenda.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam7);

					//Adicionar a linha de venda ao documento de venda
					clsDocVenda.Linhas.Insere(ClsLinhaVenda);

					ClsLinhaVenda = null;
				}


				//BID 592305 : Preencher os CDUs do cabeçalho do documento destino se existir regras definidas no Administrador
				//             e se ainda não existir CDUs preenchidos (de forma a não perder informação já existente nos CDUs)
				if (clsDocVenda.CamposUtil != null)
				{

					if (clsDocVenda.CamposUtil.NumItens == 0)
					{

						strSQL = "SELECT * FROM LigacaoCamposUtil WHERE Operacao='2' AND TabelaOrigem='CabecDoc' AND TabelaDestino='CabecDoc' AND (DocumentoOrigem='' OR DocumentoOrigem='" + TipoDocEnc + "') ORDER BY DocumentoOrigem";
						objLista = m_objErpBSO.Consulta(strSQL);

						if (!objLista.Vazia())
						{

							objLista.Inicio();

							while (!objLista.NoFim())
							{

								if (objLista.Valor("CampoOrigem").ToUpper().StartsWith("CDU_"))
								{
									//UPGRADE_WARNING: (1068) DaValorAtributo() of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
									string tempRefParam8 = objLista.Valor("CampoOrigem");
									string tempRefParam9 = objLista.Valor("CampoDestino");
									clsDocVenda.CamposUtil.GetItem(ref tempRefParam9).Valor = ReflectionHelper.GetPrimitiveValue(DaValorAtributo( FilialEnc,  TipoDocEnc,  strSerieEnc,  NumDocEnc,  tempRefParam8));
								}
								else
								{
									//UPGRADE_WARNING: (1068) CallByName() of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
									string tempRefParam10 = objLista.Valor("CampoDestino");
									clsDocVenda.CamposUtil.GetItem(ref tempRefParam10).Valor = ReflectionHelper.GetPrimitiveValue(Interaction.CallByName(clsDocVenda, objLista.Valor("CampoOrigem"), CallType.Get));
								}

								objLista.Seguinte();

							}

						}

						objLista = null;

					}

				}
				//Fim 592305

				result = clsDocVenda;
				ClsLinhaVenda = null;
				objLinhasNovas = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.SugereLinha", excep.Message);
			}

			return result;
		}

		public VndBEDocumentoVenda AdicionaLinhaTransformada( VndBEDocumentoVenda clsDocVenda,  string TipoDocEnc,  int NumDocEnc,  int NumLinEnc,  string FilialEnc,  string strSerieEnc)
		{
			double tempRefParam420 = -1;
			return AdicionaLinhaTransformada( clsDocVenda,  TipoDocEnc,  NumDocEnc,  NumLinEnc,  FilialEnc,  strSerieEnc,  tempRefParam420);
		}

		public VndBEDocumentoVenda AdicionaLinhaTransformada( VndBEDocumentoVenda clsDocVenda,  string TipoDocEnc,  int NumDocEnc,  int NumLinEnc,  string FilialEnc)
		{
			string tempRefParam421 = "";
			double tempRefParam422 = -1;
			return AdicionaLinhaTransformada( clsDocVenda,  TipoDocEnc,  NumDocEnc,  NumLinEnc,  FilialEnc,  tempRefParam421,  tempRefParam422);
		}

		public VndBEDocumentoVenda AdicionaLinhaTransformada( VndBEDocumentoVenda clsDocVenda,  string TipoDocEnc,  int NumDocEnc,  int NumLinEnc)
		{
			string tempRefParam423 = "";
			string tempRefParam424 = "";
			double tempRefParam425 = -1;
			return AdicionaLinhaTransformada( clsDocVenda,  TipoDocEnc,  NumDocEnc,  NumLinEnc,  tempRefParam423,  tempRefParam424,  tempRefParam425);
		}

		public VndBEDocumentoVenda AdicionaConversaoDocumento( VndBEDocumentoVenda clsDocVenda,  string TipoDoc,  int Numdoc,  string Filial,  string strSerie,  int Inclui)
		{

			VndBEDocumentoVenda result = null;
			VndBE100.VndBEDocumentoVenda ClsDocConverte = null;
			VndBE100.VndBELinhaDocumentoVenda clsLinha = null;
			VndBE100.VndBELinhaDocumentoVenda ClsLinhaConv = null;
			string Comentario = "";
			string strIdLinhaPaiOld = ""; //BID 598497
			string strIdLinhaPaiNew = ""; //BID 598497
			string strEstadoOrigem = "";
			string strEstadoDestino = "";
			InvBE100.InvBETipos.EnumTipoConfigEstados intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;
			string strErroEstado = "";

			try
			{

				if (Inclui == -1)
				{

					Inclui = (int) BasBETiposGcp.IncluiLinhas.vdLinhasBrancoDocsOrig;

				}

				if (Strings.Len(Filial) == 0)
				{
					Filial = clsDocVenda.Filial;
				}
				if (strSerie == "")
				{
					strSerie = clsDocVenda.Serie;
				}

				//CS.203_7.50_Alpha7 - faz a validação dos tipos de lançamento
				FuncoesComuns100.FuncoesBS.Documentos.ValidaTLConversaoDocumentos(ConstantesPrimavera100.Modulos.Vendas, clsDocVenda, TipoDoc, strSerie);

				ClsDocConverte = Edita( Filial,  TipoDoc,  strSerie,  Numdoc);

				//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				switch((BasBETiposGcp.IncluiLinhas) Inclui)
				{
					case BasBETiposGcp.IncluiLinhas.vdLinhasBrancoDocsOrig : 
						//Insere uma linha em branco no documento de venda 
						InsereLinhaComentario(clsDocVenda, ""); 
						//Insere uma linha com o comentário : Tipo e número do documento 
						//BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc] 
						Comentario = ClsDocConverte.Tipodoc + " " + ClsDocConverte.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ClsDocConverte.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(ClsDocConverte.DataDoc); 
						InsereLinhaComentario(clsDocVenda, Comentario); 
						InsereLinhaComentario(clsDocVenda, ""); 
						break;
					case BasBETiposGcp.IncluiLinhas.vdLinhasDocsOrig : 
						//Insere uma linha com o comentário : Tipo e número do documento 
						//BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc] 
						Comentario = ClsDocConverte.Tipodoc + " " + ClsDocConverte.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ClsDocConverte.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(ClsDocConverte.DataDoc); 
						InsereLinhaComentario(clsDocVenda, Comentario); 
						break;
				}

				//Adicionar cada linha do documento a converter ao documento destino
				foreach (VndBE100.VndBELinhaDocumentoVenda ClsLinhaConv2 in ClsDocConverte.Linhas)
				{
					ClsLinhaConv = ClsLinhaConv2;
					clsLinha = ClsLinhaConv;
					clsLinha.IDLinhaOriginal = ClsLinhaConv.IdLinha;

					//BID 598497
					//.IDLinha = vbNullString
					if (clsLinha.TipoLinha == ConstantesPrimavera100.Documentos.TipoLinComentario)
					{

						strIdLinhaPaiOld = clsLinha.IdLinha;
						bool tempRefParam = true;
						clsLinha.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);
						strIdLinhaPaiNew = clsLinha.IdLinha;

					}
					else
					{

						bool tempRefParam2 = true;
						clsLinha.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam2);

					}

					if (Strings.Len(clsLinha.IdLinhaPai) > 0 && clsLinha.IdLinhaPai == strIdLinhaPaiOld)
					{

						clsLinha.IdLinhaPai = strIdLinhaPaiNew;

					}
					//Fim 598497

					if (Strings.Len(clsLinha.Artigo) > 0)
					{

						if (clsLinha.Quantidade >= 0)
						{

							intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

						}
						else
						{

							intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovNegativos;

						}

						//Se os estados da linha
						string tempRefParam3 = "TipoDocSTK";
						string tempRefParam4 = clsDocVenda.Tipodoc;
						string tempRefParam5 = "TipoDocSTK";
						if (!m_objErpBSO.Vendas.TabVendas.DaValorAtributo(TipoDoc, tempRefParam3).Equals(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam4, tempRefParam5)))
						{

							FuncoesComuns100.FuncoesBS.Documentos.PreencheEstadosInventarioLinhaEstorno(clsDocVenda.Tipodoc, clsLinha);

						}
						else
						{

							if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaEstadosInventarioLinha(1, clsDocVenda.Tipodoc, clsLinha, ref strErroEstado))
							{

								m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), clsDocVenda.Tipodoc, intTipoMov, strEstadoOrigem, strEstadoDestino);
								clsLinha.INV_EstadoOrigem = strEstadoOrigem;
								clsLinha.INV_EstadoDestino = strEstadoDestino;

							}

						}

					}

					//Adiciona as linhas ao documento destino
					clsDocVenda.Linhas.Insere(clsLinha);
					ClsLinhaConv = null;
				}


				//Resultou de uma conversão de documentos
				clsDocVenda.DocsOriginais = ClsDocConverte.Tipodoc + "/" + ClsDocConverte.NumDoc.ToString();

				result = clsDocVenda;

				ClsDocConverte = null;
				ClsLinhaConv = null;
				clsLinha = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.AdicionaConversaoDocumento", excep.Message);
			}

			return result;
		}

		public VndBEDocumentoVenda AdicionaConversaoDocumento( VndBEDocumentoVenda clsDocVenda,  string TipoDoc,  int Numdoc,  string Filial,  string strSerie)
		{
			int tempRefParam426 = -1;
			return AdicionaConversaoDocumento( clsDocVenda,  TipoDoc,  Numdoc,  Filial,  strSerie,  tempRefParam426);
		}

		public VndBEDocumentoVenda AdicionaConversaoDocumento( VndBEDocumentoVenda clsDocVenda,  string TipoDoc,  int Numdoc,  string Filial)
		{
			string tempRefParam427 = "";
			int tempRefParam428 = -1;
			return AdicionaConversaoDocumento( clsDocVenda,  TipoDoc,  Numdoc,  Filial,  tempRefParam427,  tempRefParam428);
		}

		public VndBEDocumentoVenda AdicionaConversaoDocumento( VndBEDocumentoVenda clsDocVenda,  string TipoDoc,  int Numdoc)
		{
			string tempRefParam429 = "";
			string tempRefParam430 = "";
			int tempRefParam431 = -1;
			return AdicionaConversaoDocumento( clsDocVenda,  TipoDoc,  Numdoc,  tempRefParam429,  tempRefParam430,  tempRefParam431);
		}

		//Insere uma linha comentário ou em branco no objecto documento de venda.
		private void InsereLinhaComentario(VndBE100.VndBEDocumentoVenda clsDocVenda, string Comentario)
		{
			VndBE100.VndBELinhaDocumentoVenda Linha = null;

			try
			{

				Linha = new VndBE100.VndBELinhaDocumentoVenda();

				Linha.Descricao = Comentario;
				Linha.TipoLinha = "60";

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(Linha.CamposUtil, DaDefCamposUtilLinhas());

				clsDocVenda.Linhas.Insere(Linha);

				Linha = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.InsereLinhaComentario", excep.Message);
			}

		}

		//Preenche a linha do documento de venda através da linha de venda
		private void PreencheLinhaTransformada(VndBE100.VndBELinhaDocumentoVenda ClsLinhaDoc, VndBE100.VndBELinhaDocumentoVenda ClsLinhaOrigem, ref string TipoDocOrigem, ref string TipoDocDestino)
		{
			string strEstadoOrigem = "";
			string strEstadoDestino = "";
			InvBE100.InvBETipos.EnumTipoConfigEstados intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;
			bool blnMovInvertido = false;
			bool blnLigaStocks = false;
			StdBE100.StdBECampos objCampos = null;
			string strTipoMovStock = "";
			string strNTipoMovStock = "";
			GcpNumerosSerie objNumsSerie = null;
			BasBENumeroSerie objBENumSerie = null;

			try
			{

				ClsLinhaDoc.Artigo = ClsLinhaOrigem.Artigo;
				ClsLinhaDoc.Descricao = ClsLinhaOrigem.Descricao;
				ClsLinhaDoc.Armazem = ClsLinhaOrigem.Armazem;
				ClsLinhaDoc.Localizacao = ClsLinhaOrigem.Localizacao;
				ClsLinhaDoc.CodIva = ClsLinhaOrigem.CodIva;
				ClsLinhaDoc.PercIncidenciaIVA = ClsLinhaOrigem.PercIncidenciaIVA;
				ClsLinhaDoc.Comissao = ClsLinhaOrigem.Comissao;

				if (ClsLinhaDoc.DataStock != m_objErpBSO.DSO.Plat.Utils.FData(0))
				{

					if (ClsLinhaOrigem.DataEntrega != m_objErpBSO.DSO.Plat.Utils.FData(0))
					{

						ClsLinhaDoc.DataStock = DateTime.Parse(m_objErpBSO.DSO.Plat.Utils.FStr(ClsLinhaOrigem.DataEntrega) + " " + DateTimeHelper.Time.ToString("HH:mm:SS"));

					}

				}

				ClsLinhaDoc.Desconto1 = ClsLinhaOrigem.Desconto1;
				ClsLinhaDoc.Desconto2 = ClsLinhaOrigem.Desconto2;
				ClsLinhaDoc.Desconto3 = ClsLinhaOrigem.Desconto3;
				ClsLinhaDoc.DescontoComercial = ClsLinhaOrigem.DescontoComercial;
				ClsLinhaDoc.Formula = ClsLinhaOrigem.Formula;
				ClsLinhaDoc.Lote = ClsLinhaOrigem.Lote;
				ClsLinhaDoc.MovStock = ClsLinhaOrigem.MovStock;
				ClsLinhaDoc.PrecoLiquido = ClsLinhaOrigem.PrecoLiquido;
				ClsLinhaDoc.PrecUnit = ClsLinhaOrigem.PrecUnit;
				ClsLinhaDoc.QuantFormula = ClsLinhaOrigem.QuantFormula;
				ClsLinhaDoc.RegimeIva = ClsLinhaOrigem.RegimeIva;
				ClsLinhaDoc.TaxaIva = ClsLinhaOrigem.TaxaIva;
				ClsLinhaDoc.TaxaRecargo = ClsLinhaOrigem.TaxaRecargo;
				ClsLinhaDoc.TipoLinha = ClsLinhaOrigem.TipoLinha;
				ClsLinhaDoc.VariavelA = ClsLinhaOrigem.VariavelA;
				ClsLinhaDoc.VariavelB = ClsLinhaOrigem.VariavelB;
				ClsLinhaDoc.VariavelC = ClsLinhaOrigem.VariavelC;
				ClsLinhaDoc.Vendedor = ClsLinhaOrigem.Vendedor;
				ClsLinhaDoc.Unidade = ClsLinhaOrigem.Unidade;
				ClsLinhaDoc.FactorConv = ClsLinhaOrigem.FactorConv; //BID 16298

				ClsLinhaDoc.IDObra = ClsLinhaOrigem.IDObra;
				ClsLinhaDoc.WBSItem = ClsLinhaOrigem.WBSItem;
				ClsLinhaDoc.SubEmpreitada = ClsLinhaOrigem.SubEmpreitada;
				ClsLinhaDoc.ClasseActividade = ClsLinhaOrigem.ClasseActividade;
				ClsLinhaDoc.Categoria = ClsLinhaOrigem.Categoria;

				//MF BID: 531107
				ClsLinhaDoc.Ecotaxa = ClsLinhaOrigem.Ecotaxa;
				ClsLinhaDoc.CodIvaEcotaxa = ClsLinhaOrigem.CodIva;
				//UPGRADE_WARNING: (1068) m_objErpBSO.Base.IVA.DaValorAtributo() of type Variant is being forced to float. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
				ClsLinhaDoc.TaxaIvaEcotaxa = ReflectionHelper.GetPrimitiveValue<float>(m_objErpBSO.Base.Iva.DaValorAtributo(ClsLinhaOrigem.CodIvaEcotaxa, "Taxa"));
				ClsLinhaDoc.PercIvaDedutivel = ClsLinhaOrigem.PercIvaDedutivel;
				ClsLinhaDoc.PCM = ClsLinhaOrigem.PCM;
				ClsLinhaDoc.IvaNaoDedutivel = ClsLinhaOrigem.IvaNaoDedutivel;
				ClsLinhaDoc.PercIncidenciaIVA = ClsLinhaOrigem.PercIncidenciaIVA;
				ClsLinhaDoc.IvaRegraCalculo = ClsLinhaOrigem.IvaRegraCalculo;
				ClsLinhaDoc.RegimeIva = ClsLinhaOrigem.RegimeIva;
				//CS.1398
				ClsLinhaDoc.TipoOperacao = ClsLinhaOrigem.TipoOperacao;

				//BID 593372
				ClsLinhaDoc.AnaliticaCBL = ClsLinhaOrigem.AnaliticaCBL;
				ClsLinhaDoc.CCustoCBL = ClsLinhaOrigem.CCustoCBL;
				ClsLinhaDoc.ContaCBL = ClsLinhaOrigem.ContaCBL;
				ClsLinhaDoc.FuncionalCBL = ClsLinhaOrigem.FuncionalCBL;
				ClsLinhaDoc.INV_IDReserva = ClsLinhaOrigem.INV_IDReserva;
				if (Strings.Len(ClsLinhaDoc.INV_IDReserva) == 0)
				{

					ClsLinhaDoc.INV_IDReserva = DaIdReservaTransformacao(ClsLinhaDoc.IDLinhaOriginal);

				}
				ClsLinhaDoc.FactorConv = ClsLinhaOrigem.FactorConv;

				if (Strings.Len(ClsLinhaDoc.Artigo) > 0)
				{

					dynamic[] tempRefParam = new dynamic[]{"LigaStocks", "NTipoMovStk", "TipoDocSTK"};
					objCampos = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(TipoDocDestino, tempRefParam);
					if (objCampos != null)
					{

						string tempRefParam2 = "LigaStocks";
						blnLigaStocks = m_objErpBSO.DSO.Plat.Utils.FBool(objCampos.GetItem(ref tempRefParam2));
						string tempRefParam3 = "TipoDocSTK";
						strTipoMovStock = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam3));
						string tempRefParam4 = "NTipoMovStk";
						strNTipoMovStock = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam4));
						objCampos = null;

					}

					if (blnLigaStocks)
					{

						//Se a linha origem movimentou stock e o estado movimentado conta para existencias, não sugere estados, isto é,
						//não movimenta stocks
						if (!FuncoesComuns100.FuncoesBS.Documentos.LinhaMovimentaStocksTransformacao(ClsLinhaDoc.IDLinhaOriginal, ConstantesPrimavera100.Modulos.Vendas, ref TipoDocOrigem, ref TipoDocDestino, ref blnMovInvertido))
						{

							ClsLinhaDoc.INV_EstadoOrigem = "";
							ClsLinhaDoc.INV_EstadoDestino = "";

						}
						else
						{

							if (ClsLinhaDoc.Quantidade >= 0)
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

							}
							else
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovNegativos;

							}

							//Se o estado destino do movimento origem está preenchido
							if (Strings.Len(ClsLinhaOrigem.INV_EstadoDestino) != 0)
							{

								//Se os estados origem e destino estão preenchidos, o estado origem do movimento destino
								//passa a ser o destino do anterior e o destino é o estado default
								if (Strings.Len(ClsLinhaOrigem.INV_EstadoOrigem) != 0)
								{

									m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
									ClsLinhaDoc.INV_EstadoOrigem = ClsLinhaOrigem.INV_EstadoDestino;
									ClsLinhaDoc.INV_EstadoDestino = strEstadoDestino;

								}
								else
								{

									//Se o estado destino conta para existencias não preenche os estados, ou seja não movimenta
									if (!blnMovInvertido && m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Inventario.EstadosInventario.DaValorAtributo(ClsLinhaOrigem.INV_EstadoDestino, "Existencias")))
									{

										ClsLinhaDoc.INV_EstadoOrigem = "";
										ClsLinhaDoc.INV_EstadoDestino = "";

									}
									else
									{

										//Se o estado destino não conta para existencias, o estado origem do movimento destino
										//passa a ser o destino do anterior e o destino é o estado default
										//Obter os estados default
										m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
										ClsLinhaDoc.INV_EstadoOrigem = ClsLinhaOrigem.INV_EstadoDestino;
										ClsLinhaDoc.INV_EstadoDestino = strEstadoDestino;

									}

								}

							}
							else
							{

								if (!blnMovInvertido && Strings.Len(ClsLinhaOrigem.INV_EstadoOrigem) != 0)
								{

									ClsLinhaDoc.INV_EstadoOrigem = "";
									ClsLinhaDoc.INV_EstadoDestino = "";

								}
								else
								{

									//Caso contrário vou usar os estados default configurados para o documento
									//Obter os estados default
									m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
									ClsLinhaDoc.INV_EstadoOrigem = strEstadoOrigem;
									ClsLinhaDoc.INV_EstadoDestino = strEstadoDestino;

								}

							}

						}

					}

					objNumsSerie = new GcpNumerosSerie();
					string tempRefParam5 = ClsLinhaDoc.Lote;
					FuncoesComuns100.FuncoesBS.Documentos.CarregaNumerosSerieDocOrigem(objNumsSerie, ClsLinhaDoc.Artigo, ConstantesPrimavera100.Modulos.Vendas, ClsLinhaDoc.IdLinha, ClsLinhaDoc.IDLinhaOriginal, ClsLinhaDoc.Armazem, ClsLinhaDoc.Localizacao, "", ref tempRefParam5);

					//Obter os números de série movimentados na reserva
					DaNumerosSerieTransformacaoReserva(ClsLinhaDoc, objNumsSerie);

					foreach (GcpNumeroSerie objNSerie in objNumsSerie)
					{

						objBENumSerie = new BasBENumeroSerie();
						objBENumSerie.Modulo = ConstantesPrimavera100.Modulos.Vendas;
						objBENumSerie.NumeroSerie = objNSerie.NumeroSerie;
						objBENumSerie.IdNumeroSerie = objNSerie.IdNumeroSerie;
						ClsLinhaDoc.NumerosSerie.Insere(objBENumSerie);

					}

					objBENumSerie = null;
					objNumsSerie = null;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.TransformaLinha", excep.Message);
			}


		}


		//---------------------------------------------------------------------------------------
		// Procedure   : DaNumerosSerieTransformacaoReserva
		// Description : Devolve os números de série movimentados na reserva
		// Arguments   : LinhaDoc  -->
		// Arguments   : NumsSerie -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void DaNumerosSerieTransformacaoReserva(VndBE100.VndBELinhaDocumentoVenda LinhaDoc, GcpNumerosSerie NumsSerie)
		{
			GcpNumeroSerie objNSerie = null;
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			string strNumSerie = "";
			string strIdNumSerie = "";

			try
			{

				if (LinhaDoc == null)
				{

					return;

				}



				if (Strings.Len(LinhaDoc.INV_IDReserva) > 0)
				{

					strSQL = "SELECT NSM.IdNumeroSerie, NSM.NumeroSerie from INV_Movimentos (NOLOCK) M" + Environment.NewLine;
					strSQL = strSQL + " INNER JOIN INV_NumerosSerieMovimento (NOLOCK) NSM ON NSM.IdMovimentoStock = M.Id" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN INV_NumerosSerie (NOLOCK) NS ON NS.Id = NSM.IdNumeroSerie AND NS.Stock = 1" + Environment.NewLine;
					strSQL = strSQL + "WHERE M.IdReserva = '@1@' AND M.TipoMovimento = 'E'" + Environment.NewLine;
					dynamic[] tempRefParam = new dynamic[]{LinhaDoc.INV_IDReserva};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

					objLista = m_objErpBSO.Consulta(strSQL);
					dynamic tempRefParam2 = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam2))
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;

						while (!objLista.NoFim())
						{

							strNumSerie = m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("NumeroSerie"));
							strIdNumSerie = m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("IdNumeroSerie"));

							// Adiciona o número de série à colecção
							if (NumsSerie.GetChave(((int) BasBETipos.RegrasDescPrec.Artigo).ToString(), strNumSerie, strIdNumSerie, LinhaDoc.Armazem, LinhaDoc.Localizacao, "") == null)
							{ //PriGlobal: IGNORE

								objNSerie = NumsSerie.Add(LinhaDoc.Artigo, strNumSerie, strIdNumSerie, LinhaDoc.Armazem, LinhaDoc.Localizacao, ""); //PriGlobal: IGNORE
								objNSerie.Lote = LinhaDoc.Lote;
								objNSerie.Linhaid = LinhaDoc.IdLinha;
								objNSerie = null;

							}

							objLista.Seguinte();

						}
						objLista = null;

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;
					}

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_DaNumerosSerieTransformacaoReserva", excep.Message);
			}


		}

		private string DaIdReservaTransformacao(string IDLinhaOriginal)
		{
			string result = "";
			dynamic objReserva = null;
			string strIdTipoOrigem = "";

			try
			{

				strIdTipoOrigem = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));
				objReserva = (dynamic) m_objErpBSO.Inventario.Reservas.EditaDestino(strIdTipoOrigem, IDLinhaOriginal);
				if (objReserva.Linhas.NumItens > 0)
				{

					result = objReserva.Linhas.GetEdita(1).ID;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_DaIdReservaTransformacao", excep.Message);
			}

			return result;
		}

		//UPGRADE_NOTE: (7001) The following declaration (Insere) seems to be dead code More Information: http://www.vbtonet.com/ewis/ewi7001.aspx
		//private void Insere(string Filial, string TipoDoc, string strSerie, int Numdoc, ref string[, ] Arr)
		//{
			//int Pos = 0;
			//
			//try
			//{
				//
				//Última posição do array
				//Pos = Arr.GetUpperBound(1) + 1;
				//Arr = ArraysHelper.RedimPreserve<string[, ]>(Arr, new int[]{5, Pos + 1});
				//
				//Arr[1, Pos] = Filial;
				//Arr[2, Pos] = strSerie;
				//Arr[3, Pos] = TipoDoc;
				//Arr[4, Pos] = Numdoc.ToString();
			//}
			//catch (System.Exception excep)
			//{
				//
				////UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				//StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.Insere", excep.Message);
			//}
			//
		//}

		public StdBELista LstUltPrecoArtigoCliente(string Vendedor)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.LstUltPrecoArtigoCliente(ref Vendedor);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstUltPrecoArtigoCliente", excep.Message);
			}
			return null;
		}

		public void AdicionaLinhaEspecial( VndBEDocumentoVenda clsDoc,  vdTipoLinhaEspecial TipoLinha,  double PrecUnit,  string Descricao)
		{
			VndBE100.VndBELinhaDocumentoVenda clsLinha = null;
			string strDescricao = "";

			try
			{

				clsLinha = new VndBE100.VndBELinhaDocumentoVenda();

				strDescricao = Descricao;



				switch(TipoLinha)
				{
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_Acerto : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinAcertos, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinAcertos, PrecUnit); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_Comentario : 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinComentario); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_DescValMercadorias : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinDescontoMercadorias, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinDescontoMercadorias, PrecUnit); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_DescValServicos : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinDescServicos, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinDescServicos, PrecUnit); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_OutrosServicos : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinOutrosServicos, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinOutrosServicos, PrecUnit); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_portes : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinPortes, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinPortes, PrecUnit); 
						 
						break;
					case BasBETiposGcp.vdTipoLinhaEspecial.vdLinha_Adiantamentos : 
						 
						if (Strings.Len(strDescricao) == 0)
						{
							//UPGRADE_WARNING: (1068) m_objErpBSO.Base.TiposMovimento.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
							strDescricao = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.TiposMovimento.DaValorAtributo(ConstantesPrimavera100.Documentos.TipoLinAdiantamentos, "Descricao"));
						} 
						 
						clsLinha = AdicionaTipoLinha(clsDoc, strDescricao, ConstantesPrimavera100.Documentos.TipoLinAdiantamentos, PrecUnit); 
						 
						break;
				}


				//Adiciona a linha ao documento de venda
				clsDoc.Linhas.Insere(clsLinha);

				clsLinha = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.AdicionaLinhaEspecial", excep.Message);
			}

		}

		public void AdicionaLinhaEspecial( VndBEDocumentoVenda clsDoc, vdTipoLinhaEspecial TipoLinha,  double PrecUnit)
		{
			string tempRefParam432 = "";
			AdicionaLinhaEspecial( clsDoc,  TipoLinha,  PrecUnit,  tempRefParam432);
		}

		public void AdicionaLinhaEspecial( VndBEDocumentoVenda clsDoc, vdTipoLinhaEspecial TipoLinha)
		{
			double tempRefParam433 = 0;
			string tempRefParam434 = "";
			AdicionaLinhaEspecial( clsDoc,  TipoLinha,  tempRefParam433,  tempRefParam434);
		}

		private VndBE100.VndBELinhaDocumentoVenda AdicionaTipoLinha(VndBE100.VndBEDocumentoVenda clsDoc, string Descricao, string TipoLinha, double PrecUnit = 0)
		{

			VndBE100.VndBELinhaDocumentoVenda result = null;
			VndBE100.VndBELinhaDocumentoVenda clsLinha = null;
			StdBE100.StdBECampos objCampos = null;
			int TipoMercado = 0;
			//CS.1398
			StdBE100.StdBECampos objCamposEntidade = null;
			int intRegimeIva = 0;

			try
			{

				clsLinha = new VndBE100.VndBELinhaDocumentoVenda();

				clsLinha.Descricao = Descricao;
				clsLinha.TipoLinha = TipoLinha;
				clsLinha.PrecUnit = PrecUnit;
				if (TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario && TipoLinha != ConstantesPrimavera100.Documentos.TipoLinAcertos)
				{
					//BID 535927
					//            .CodIva = m_objErpBSO.Base.Params.CodigoIvaPortes
					//UPGRADE_WARNING: (6021) Casting 'string' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
					BasBETipos.LOGEspacoFiscalDoc switchVar = (BasBETipos.LOGEspacoFiscalDoc) Convert.ToInt32(Double.Parse(clsDoc.RegimeIva));
					if (switchVar == BasBETipos.LOGEspacoFiscalDoc.MercadoExterno)
					{
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaExterno;
					}
					else if (switchVar == BasBETipos.LOGEspacoFiscalDoc.MercadoIntracomunitario)
					{ 
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaIntracom;
						clsLinha.IvaRegraCalculo = 1;
					}
					else if (switchVar == BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva)
					{ 
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaIsento;
					}
					else
					{
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaPortes;
					}
					//Fim 535927
					objCampos = m_objErpBSO.Base.Iva.DaValorAtributos(clsLinha.CodIva, "Taxa", "TaxaRecargo");
					if (objCampos != null)
					{
						clsLinha.PercIncidenciaIVA = 100;
						string tempRefParam = "Taxa";
						clsLinha.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam).Valor);
						if (FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoLinhaParaRecargo(clsLinha.TipoLinha))
						{
							string tempRefParam2 = "TaxaRecargo";
							clsLinha.TaxaRecargo = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam2).Valor);
						}
						else
						{
							clsLinha.TaxaRecargo = 0;
						}
						objCampos = null;
					}
					else
					{
						clsLinha.CodIva = "";
						clsLinha.PercIncidenciaIVA = 0;
						clsLinha.TaxaIva = 0;
						clsLinha.TaxaRecargo = 0;
					}
					clsLinha.PercIvaDedutivel = 100; //BID 543791
				}
				//Apenas quando é comentário é que a quantidade é 1
				if (TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario)
				{
					clsLinha.Quantidade = 1;
				}

				clsLinha.Lote = ConstantesPrimavera100.Inventario.LotePorDefeito;
				clsLinha.PrecUnit = PrecUnit;
				if (Strings.Len(clsDoc.RegimeIva) > 0)
				{
					clsLinha.RegimeIva = clsDoc.RegimeIva;
				}
				else
				{
					if (clsDoc.TipoEntidade == "C")
					{

						objCamposEntidade = m_objErpBSO.Base.Clientes.DaValorAtributos(clsDoc.Entidade, "TipoCli", "RegimeIvaReembolsos");

						if (objCamposEntidade != null)
						{

							string tempRefParam3 = "TipoCli";
							TipoMercado = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposEntidade.GetItem(ref tempRefParam3)); //PriGlobal: IGNORE
							string tempRefParam4 = "RegimeIvaReembolsos";
							intRegimeIva = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposEntidade.GetItem(ref tempRefParam4)); //PriGlobal: IGNORE

						}
					}
					else
					{

						objCamposEntidade = m_objErpBSO.Base.OutrosTerceiros.DaValorAtributos(clsDoc.Entidade, clsDoc.TipoEntidade, "TipoTerc", "RegimeIvaReembolsos");

						if (objCamposEntidade != null)
						{

							string tempRefParam5 = "TipoCli";
							TipoMercado = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposEntidade.GetItem(ref tempRefParam5)); //PriGlobal: IGNORE
							string tempRefParam6 = "RegimeIvaReembolsos";
							intRegimeIva = m_objErpBSO.DSO.Plat.Utils.FInt(objCamposEntidade.GetItem(ref tempRefParam6)); //PriGlobal: IGNORE

						}

					}

					//CS.1398
					clsLinha.RegimeIva = m_objErpBSO.DSO.Plat.Utils.FStr((int) FuncoesComuns100.FuncoesBS.Documentos.DevolveEspacoFiscalCalculado(TipoMercado.ToString(), intRegimeIva, m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, clsDoc.Tipodoc, clsDoc.Serie, "IvaIncluido")), ConstantesPrimavera100.Modulos.Vendas));
					//END CS.1398

				}

				//BID 535927
				if (TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario && TipoLinha != ConstantesPrimavera100.Documentos.TipoLinAcertos)
				{
					//UPGRADE_WARNING: (6021) Casting 'string' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
					BasBETipos.LOGEspacoFiscalDoc switchVar_2 = (BasBETipos.LOGEspacoFiscalDoc) Convert.ToInt32(Double.Parse(clsLinha.RegimeIva));
					if (switchVar_2 == BasBETipos.LOGEspacoFiscalDoc.MercadoExterno)
					{
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaExterno;

					}
					else if (switchVar_2 == BasBETipos.LOGEspacoFiscalDoc.MercadoIntracomunitario)
					{ 
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaIntracom;
						clsLinha.IvaRegraCalculo = 1;

					}
					else if (switchVar_2 == BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva)
					{ 
						clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaIsento;

					}
					else
					{
						if (m_objErpBSO.Base.Iva.Existe(m_objErpBSO.Base.Params.CodigoIvaPortes))
						{
							clsLinha.CodIva = m_objErpBSO.Base.Params.CodigoIvaPortes;
						}
					}

					objCampos = m_objErpBSO.Base.Iva.DaValorAtributos(clsLinha.CodIva, "Taxa", "TaxaRecargo");
					if (objCampos != null)
					{
						clsLinha.PercIncidenciaIVA = 100;
						string tempRefParam7 = "Taxa";
						clsLinha.TaxaIva = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam7).Valor);
						if (FuncoesComuns100.FuncoesBS.Documentos.ValidaTipoLinhaParaRecargo(clsLinha.TipoLinha))
						{
							string tempRefParam8 = "TaxaRecargo";
							clsLinha.TaxaRecargo = ReflectionHelper.GetPrimitiveValue<float>(objCampos.GetItem(ref tempRefParam8).Valor);
						}
						else
						{
							clsLinha.TaxaRecargo = 0;
						}
						objCampos = null;
					}
					else
					{
						clsLinha.CodIva = "";
						clsLinha.PercIncidenciaIVA = 0;
						clsLinha.TaxaIva = 0;
						clsLinha.TaxaRecargo = 0;
					}
				}
				//^BID 535927

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(clsLinha.CamposUtil, DaDefCamposUtilLinhas());


				result = clsLinha;
				clsLinha = null;
				objCamposEntidade = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.AdicionaTipoLinha", excep.Message);
			}

			return result;
		}

		private string[] DaArrayCampos(OrderedDictionary colCampos)
		{

			string[] arrCampos = ArraysHelper.InitializeArray<string>(colCampos.Count + 3);

			arrCampos[0] = "Entidade"; //PriGlobal: IGNORE
			arrCampos[1] = "Moeda"; //PriGlobal: IGNORE
			arrCampos[2] = "EntidadeFac"; //BID 590916 'PriGlobal: IGNORE

			for (int intPos = 1; intPos <= colCampos.Count; intPos++)
			{
				arrCampos[intPos + 2] = (string) colCampos[intPos - 1];
			}

			return arrCampos;
		}

		private bool VerificaAgrupamento(StdBE100.StdBECampos objCampos, VndBE100.VndBEDocumentoVenda objDocVendaDest, string strSerieOrig, string strSerieDest)
		{
			int intPos = 0;

			bool blnVerifica = (strSerieOrig == strSerieDest);

			int tempForVar = objCampos.NumItens;
			string tempRefParam = intPos.ToString();
			StdBE100.StdBECampo withVar = objCampos.GetItem(ref tempRefParam);
			intPos = Convert.ToInt32(Double.Parse(tempRefParam));
			for (intPos = 1; intPos <= tempForVar; intPos++)
			{
				if (!blnVerifica)
				{
					break;
				}

				switch(withVar.Nome.ToUpper())
				{
					case "ENTIDADE" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Entidade);  //PriGlobal: IGNORE 
						break;
					case "MOEDA" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Moeda);  //PriGlobal: IGNORE 
						break;
					case "ENTIDADEFAC" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.EntidadeFac);  //BID 590916 'PriGlobal: IGNORE 
						break;
					case "CONDPAG" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.CondPag);  //PriGlobal: IGNORE 
						break;
					case "MODOPAG" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.ModoPag);  //PriGlobal: IGNORE 
						break;
					case "MODOEXP" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.ModoExp);  //PriGlobal: IGNORE 
						break;
					case "MORADA" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Morada);  //PriGlobal: IGNORE 
						break;
					case "LOCALIDADE" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Localidade);  //PriGlobal: IGNORE 
						break;
					case "CODPOSTAL" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.CodigoPostal);  //PriGlobal: IGNORE 
						break;
					case "CODPOSTALLOCALIDADE" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.LocalidadeCodigoPostal);  //PriGlobal: IGNORE 
						break;
					case "MORADAALTENTREGA" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.MoradaAlternativaEntrega);  //PriGlobal: IGNORE 
						break;
					case "LOCALDESCARGA" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.LocalDescarga);  //PriGlobal: IGNORE 
						break;
					case "REQUISICAO" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Requisicao);  //PriGlobal: IGNORE 
						break;
					case "NUMCONTRIBUINTE" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.NumContribuinte);  //PriGlobal: IGNORE 
						break;
					case "GRUPO" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Grupo);  //PriGlobal: IGNORE 
						break;
					case "ORIGEM" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Origem);  //PriGlobal: IGNORE 
						break;
					case "LOCALOPERACAO" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.LocalOperacao);  //PriGlobal: IGNORE 
						break;
					case "REFERENCIA" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.Referencia);  //PriGlobal: IGNORE 
						break;
					case "CONTADOMICILIACAO" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.ContaDomiciliacao);  //PriGlobal: IGNORE 
						break;
					case "CONTRATOID" :  
						blnVerifica = (ReflectionHelper.GetPrimitiveValue<string>(withVar.Valor) == objDocVendaDest.IdContrato);  //PriGlobal: IGNORE 
						 
						break;
					default:
						if (withVar.Nome.ToUpper().StartsWith("CDU_"))
						{
							//BID 562772
							//blnVerifica = (.Valor = objDocVendaDest.CamposUtil(.Nome).Valor)
							//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
							string tempRefParam2 = withVar.Nome;
							string tempRefParam3 = withVar.Nome;
							blnVerifica = (((Convert.IsDBNull(withVar.Valor)) ? ((object) "") : ReflectionHelper.GetPrimitiveValue(withVar.Valor)) == ((Convert.IsDBNull(objDocVendaDest.CamposUtil.GetItem(ref tempRefParam2).Valor)) ? ((object) "") : ReflectionHelper.GetPrimitiveValue(objDocVendaDest.CamposUtil.GetItem(ref tempRefParam3).Valor)));
							//Fim 562772
						} 
						break;
				}
			}

			return blnVerifica;
		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk,  System.DateTime DataEmissao,  bool IncluiComentarios,  OrderedDictionary colIdProjectos,  OrderedDictionary colDocumentosGerados, bool LotesAutomaticos)
		{
			string MsgErro = "";
			int NumDocProximo = 0;
			System.DateTime DataProxima = DateTime.FromOADate(0);
			int i = 0;
			int f = 0;
			string strDocConsiderados = "";
			StdBE100.StdBECampos objCamposTipoDocOrig = null;
			StdBE100.StdBECampos objCamposTipoDocDest = null;
			bool MovStocks = false;
			string strMsg = "";
			string MsgResult = "";
			VndBE100.VndBEDocumentoVenda objDocVendaDest = null;
			VndBE100.VndBEDocumentoVenda ObjDocVendaOrig = null;
			StdBE100.StdBECampos objCampos = null;
			System.DateTime DataDocOrig = DateTime.FromOADate(0);
			int intNPrestacoes = 0;
			OrderedDictionary colIDLinhas = null;
			bool blnIniciouTrans = false; //BID 539947
			bool blnErroNoActualiza = false; //BID 541522
			OrderedDictionary colDocs = null; //BID 548479
			string strDocOrig = ""; //BID 573232
			string strID = ""; //BID 583409
			BasBETiposGcp.TPDocumentos DocsGerados = new BasBETiposGcp.TPDocumentos();
			System.DateTime dtHoraCarga = DateTime.FromOADate(0);

			//try
			//{

			//	colIDLinhas = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

			//	blnErroNoActualiza = false; //BID 541522

			//	// VALIDA CONVERSAO
			//	if (ValidaConversao(DocsVenda, TipoDocDestino, strSerieDestino, NumDocProximo, AgruparObjectos, ref MsgErro))
			//	{

			//		//US 27510 - Consumo automatico de lotes na conversão de documentos
			//		if (LotesAutomaticos)
			//		{

			//			m_clsLotesAuto = new clsLotesAuto();
			//			m_clsLotesAuto.Motor = m_objErpBSO;
			//			m_clsLotesAuto.AccaoRupturaStk = AccaoRupturaStk;

			//		}

			//		i = 1;
			//		colDocumentosGerados = new OrderedDictionary();

   //                 while (i <= DocsVenda.Count)
   //                 {
   //                     VndBEDocumentoVenda objDocumento = DocsVenda.GetItem(i) as VndBEDocumentoVenda;

   //                     m_objErpBSO.IniciaTransaccao();
   //                     blnIniciouTrans = true;

   //                     //UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
   //                     if (DataEmissao.Equals(DateTime.FromOADate(0)))
   //                     {
   //                         //DataProxima = m_objErpBSO.Base.Series.SugereDataDocumento("V", ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i)), ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i)));
   //                     }
   //                     else
   //                     {
   //                         DataProxima = DataEmissao;
   //                     }

   //                     //NumDocProximo = m_objErpBSO.Base.Series.ProximoNumero("V", ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i)), ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i)), true);

   //                     //string tempParam = ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                     string tempParam2 = "Data";
   //                     string tempParam3 = Convert.ToString(objDocumento.IdDocOrigem);
   //                     //if (ValidaConversaoNumero( tempParam, ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i)),  NumDocProximo, ReflectionHelper.GetPrimitiveValue<System.DateTime>(DaValorAtributosId( tempParam3, "Data").GetItem( tempParam2).Valor), DataProxima,  MsgErro))
   //                     {
   //                         objDocumento.IdDocOrigem = tempParam3; //PriGlobal: IGNORE

   //                         string tempParam4 = Convert.ToString(objDocumento.IdDocOrigem);
   //                         ObjDocVendaOrig = EditaID(tempParam4);
   //                         objDocumento.IdDocOrigem = tempParam4;

   //                         // Inicializa o documento destino
   //                         objDocVendaDest = ObjDocVendaOrig;

   //                         //Data do documento original para usar nas mensagens
   //                         DataDocOrig = ObjDocVendaOrig.DataDoc;
   //                         objDocVendaDest.DataDoc = DataProxima;
   //                         strDocConsiderados = "";

   //                         string tempParam5 = ""; //ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                         dynamic[] tempParam6 = new dynamic[] { "LigaStocks", "LigaCC", "PagarReceber", "TipoDocumento", "PendentePorLinha" };
   //                         objCamposTipoDocDest = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(tempParam5, tempParam6);
   //                         string tempParam7 = ObjDocVendaOrig.Tipodoc;
   //                         dynamic[] tempParam8 = new dynamic[] { "LigaStocks", "LigaCC", "PagarReceber", "TipoDocumento", "BensCirculacao" };
   //                         objCamposTipoDocOrig = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(tempParam7, tempParam8);

   //                         string tempParam9 = "LigaStocks";
   //                         string tempParam10 = "LigaStocks";
   //                         string tempParam11 = "LigaStocks";
   //                         string tempParam12 = "PagarReceber";
   //                         string tempParam13 = "PagarReceber";
   //                         MovStocks = (ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam9).Valor) & (~ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam10).Valor) | (ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam11).Valor) & ((!objCamposTipoDocOrig.GetItem(ref tempParam12).Valor.Equals(objCamposTipoDocDest.GetItem(ref tempParam13).Valor)) ? -1 : 0)))) != 0; //PriGlobal: IGNORE


   //                         //Se seleccionamos a opção data de carga, a data de carga será a data seleccionada
   //                         if (Strings.Len(Convert.ToString(objDocumento.DataCarga)) != 0)
   //                         {

   //                             objDocVendaDest.DataCarga = Convert.ToString(objDocumento.DataCarga);

   //                             if (Strings.Len(Convert.ToString(objDocumento.HoraCarga)) != 0)
   //                             {

   //                                 objDocVendaDest.HoraCarga = Convert.ToString(objDocumento.HoraCarga);

   //                             }
   //                             else
   //                             {

   //                                 dtHoraCarga = DateTime.Now.AddMinutes(2);
   //                                 objDocVendaDest.HoraCarga = StringsHelper.Format(DateAndTime.Hour(dtHoraCarga), "00") + ":" + StringsHelper.Format(dtHoraCarga.Minute, "00");

   //                             }

   //                             if (Strings.Len(Convert.ToString(objDocumento.Matricula)) != 0)
   //                             {
   //                                 objDocVendaDest.Matricula = Convert.ToString(objDocumento.Matricula);
   //                             }
   //                             else
   //                             {
   //                                 objDocVendaDest.Matricula = objDocVendaDest.Matricula;
   //                             }

   //                             //Se não seleccionou a data de carga o comportamento deverá ser o seguinte:
   //                             //1 - Se o documento destino movimenta stocks e o origem não movimenta então temos que calcular a data de carga;
   //                             //2 - A data de carga será calculada da seguinte forma: se a data documento for anterior à data actual a data de carga será a data do documento;
   //                             //    Caso contrário será a data da conversão
   //                             //3 - Se o Documento de Origem não movimentar Stocks e o de Destino também não movimentar ou se o Documento de Origem
   //                             //    movimentar Stocks e o de Destino também movimentar, a Data de Carga e a Data de Descarga serão copiadas do Documentos de Origem
   //                         }
   //                         else
   //                         {

   //                             if (MovStocks)
   //                             {

   //                                 //Se indicou uma data de emissão no passado a data de carga deverá ser essa data
   //                                 if (objDocVendaDest.DataDoc < DateTime.Parse(DateTimeHelper.ToString(DateTime.Now)))
   //                                 {
   //                                     objDocVendaDest.DataCarga = DateTimeHelper.ToString(objDocVendaDest.DataDoc);
   //                                 }
   //                                 else
   //                                 {
   //                                     objDocVendaDest.DataCarga = DateTime.Now.ToString("d");
   //                                 }

   //                                 objDocVendaDest.HoraCarga = Strings.FormatDateTime(DateTime.Now, DateFormat.ShortTime);
   //                                 objDocVendaDest.Matricula = objDocVendaDest.Matricula;

   //                             }
   //                             else
   //                             {

   //                                 objDocVendaDest.DataCarga = objDocVendaDest.DataCarga;
   //                                 objDocVendaDest.HoraCarga = objDocVendaDest.HoraCarga;
   //                                 objDocVendaDest.Matricula = objDocVendaDest.Matricula;

   //                             }

   //                         }

   //                         if (((m_objErpBSO.DSO.Plat.Utils.FData(objDocVendaDest.DataCarga) > m_objErpBSO.DSO.Plat.Utils.FData(objDocVendaDest.DataDescarga)) || (m_objErpBSO.DSO.Plat.Utils.FData(objDocVendaDest.DataCarga) == m_objErpBSO.DSO.Plat.Utils.FData(objDocVendaDest.DataDescarga) && String.CompareOrdinal(objDocVendaDest.HoraCarga, objDocVendaDest.HoraDescarga) > 0 && objDocVendaDest.HoraDescarga != "")) && Information.IsDate(objDocVendaDest.DataDescarga))
   //                         {
   //                             objDocVendaDest.DataDescarga = "";
   //                             objDocVendaDest.HoraDescarga = "";
   //                         }


   //                         //UPGRADE_WARNING: (6021) Casting 'Variant' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
   //                         string tempParam14 = "LigaCC";
   //                         string tempParam15 = "LigaCC";
   //                         string tempParam16 = "TipoDocumento";
   //                         if ((ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam14).Valor) & ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam15).Valor) & ((((BasBETipos.LOGTipoDocumento)ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam16).Valor)) < BasBETipos.LOGTipoDocumento.LOGDocFinanceiro) ? -1 : 0)) != 0)
   //                         { //PriGlobal: IGNORE

   //                             intNPrestacoes = 1;

   //                             if (ObjDocVendaOrig.Prestacoes.NumItens > 1)
   //                             {
   //                                 intNPrestacoes = ObjDocVendaOrig.Prestacoes.NumItens;
   //                             }

   //                             for (int intPrestacao = 1; intPrestacao <= intNPrestacoes; intPrestacao++)
   //                             {
   //                                 m_objErpBSO.PagamentosRecebimentos.Pendentes.Remove(ObjDocVendaOrig.Filial, "V", ObjDocVendaOrig.Tipodoc, ObjDocVendaOrig.Serie, ObjDocVendaOrig.NumDoc, intPrestacao, 0, true);
   //                             }

   //                         }


   //                         if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefTipoDocOrig)) > 0 || Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefSerieDocOrig)) > 0 || Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefSerieDocOrig)) > 0)
   //                         {
   //                             strDocOrig = " (" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16060, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.RefTipoDocOrig + " " + ObjDocVendaOrig.RefSerieDocOrig + " " + ObjDocVendaOrig.RefSerieDocOrig + ")";
   //                         }
   //                         else
   //                         {
   //                             strDocOrig = "";
   //                         }

   //                         if (ObjDocVendaOrig.Filial == m_objErpBSO.Base.Filiais.CodigoFilial)
   //                         {
   //                             //A descrição deve estar no formato [TipoDoc Serie/NumDoc]
   //                             string tempParam17 = ""; // ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                             PreencheLinhasConv(objDocVendaDest, ObjDocVendaOrig.Linhas, LinhaSeparadora, LinhaBranco, false, ObjDocVendaOrig.Tipodoc + " " + ObjDocVendaOrig.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(DataDocOrig) + strDocOrig, MovStocks, AccaoRupturaStk, ref MsgResult, ref strDocConsiderados, DataDocOrig, IncluiComentarios, colIDLinhas, colIdProjectos, LotesAutomaticos, ref tempParam17);
   //                         }
   //                         else
   //                         {
   //                             //BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc]
   //                             string tempParam18 = ""; // ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                             PreencheLinhasConv(objDocVendaDest, ObjDocVendaOrig.Linhas, LinhaSeparadora, LinhaBranco, false, ObjDocVendaOrig.Tipodoc + " " + ObjDocVendaOrig.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9827, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.Filial + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(DataDocOrig) + strDocOrig, MovStocks, AccaoRupturaStk, ref MsgResult, ref strDocConsiderados, DataDocOrig, IncluiComentarios, colIDLinhas, colIdProjectos, LotesAutomaticos, ref tempParam18);
   //                         }

   //                         ObjDocVendaOrig = null;

   //                         if (AgruparObjectos)
   //                         {

   //                             f = i + 1;
   //                             while (f <= DocsVenda.Count)
   //                             {

   //                                 string tempParam19 = Convert.ToString(objDocumento.IdDocOrigem);
   //                                 Array tempParam20 = DaArrayCampos(strCamposAgrupamento);
   //                                 objCampos = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosID(tempParam19, tempParam20);
   //                                 objDocumento.IdDocOrigem = tempParam19; //PriGlobal: IGNORE

   //                                 if (VerificaAgrupamento(objCampos, objDocVendaDest, ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i)), ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(f))))
   //                                 {

   //                                     string tempParam21 = Convert.ToString(objDocumento.IdDocOrigem);
   //                                     ObjDocVendaOrig = EditaID(tempParam21);
   //                                     objDocumento.IdDocOrigem = tempParam21;

   //                                     //BID 573232
   //                                     if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefTipoDocOrig)) > 0 || Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefSerieDocOrig)) > 0 || Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(ObjDocVendaOrig.RefDocOrig)) > 0)
   //                                     {
   //                                         strDocOrig = " (" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16060, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.RefTipoDocOrig + " " + ObjDocVendaOrig.RefSerieDocOrig + " " + ObjDocVendaOrig.RefDocOrig + ")";
   //                                     }
   //                                     else
   //                                     {
   //                                         strDocOrig = "";
   //                                     }

   //                                     //BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc]
   //                                     string tempParam22 = ""; // ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                                     PreencheLinhasConv(objDocVendaDest, ObjDocVendaOrig.Linhas, LinhaSeparadora, LinhaBranco, true, ObjDocVendaOrig.Tipodoc + " " + ObjDocVendaOrig.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + ObjDocVendaOrig.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(ObjDocVendaOrig.DataDoc) + strDocOrig, MovStocks, AccaoRupturaStk, ref MsgResult, ref strDocConsiderados, ObjDocVendaOrig.DataDoc, IncluiComentarios, colIDLinhas, colIdProjectos, LotesAutomaticos, ref tempParam22);

   //                                     objDocVendaDest.TotalMerc += ObjDocVendaOrig.TotalMerc;
   //                                     objDocVendaDest.TotalDesc += ObjDocVendaOrig.TotalDesc;
   //                                     objDocVendaDest.TotalIva += ObjDocVendaOrig.TotalIva;
   //                                     objDocVendaDest.TotalOutros += ObjDocVendaOrig.TotalOutros;
   //                                     objDocVendaDest.TotalRetencao += ObjDocVendaOrig.TotalRetencao;
   //                                     objDocVendaDest.TotalDocumento += ObjDocVendaOrig.TotalDocumento;

   //                                     //BID 552636
   //                                     if (Information.IsDate(objDocVendaDest.DataDescarga))
   //                                     {

   //                                         objDocVendaDest.DataDescarga = "";
   //                                         objDocVendaDest.HoraDescarga = "";

   //                                     }

   //                                     //UPGRADE_WARNING: (6021) Casting 'Variant' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
   //                                     string tempParam23 = "LigaCC";
   //                                     string tempParam24 = "LigaCC";
   //                                     string tempParam25 = "TipoDocumento";
   //                                     if ((ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam23).Valor) & ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam24).Valor) & ((((BasBETipos.LOGTipoDocumento)ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam25).Valor)) < BasBETipos.LOGTipoDocumento.LOGDocFinanceiro) ? -1 : 0)) != 0)
   //                                     { //PriGlobal: IGNORE
   //                                       // REMOVE OS PENDENTES CASO O DESTINO TENHA LIGACAO A CONTAS CORRENTES

   //                                         intNPrestacoes = 1;

   //                                         if (ObjDocVendaOrig.Prestacoes.NumItens > 1)
   //                                         {
   //                                             intNPrestacoes = ObjDocVendaOrig.Prestacoes.NumItens;
   //                                         }

   //                                         for (int intPrestacao = 1; intPrestacao <= intNPrestacoes; intPrestacao++)
   //                                         {
   //                                             m_objErpBSO.PagamentosRecebimentos.Pendentes.Remove(ObjDocVendaOrig.Filial, "V", ObjDocVendaOrig.Tipodoc, ObjDocVendaOrig.Serie, ObjDocVendaOrig.NumDoc, intPrestacao, 0, true);
   //                                         }

   //                                     }

   //                                     ObjDocVendaOrig = null;

   //                                     DocsVenda.RemoveAt(f - 1);
   //                                     TipoDocDestino.RemoveAt(f - 1);
   //                                     strSerieDestino.RemoveAt(f - 1);

   //                                 }
   //                                 else
   //                                 {
   //                                     f++;
   //                                 }

   //                                 objCampos = null;

   //                             }

   //                             //BID 548479
   //                             colDocs = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
   //                             string tempParam26 = ""; // ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i));
   //                             if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaLinhasNegativas(objDocVendaDest, colDocs, ref tempParam26, ref MsgErro, ref i, ConstantesPrimavera100.Modulos.Vendas))
   //                             {
   //                                 colDocs = null;
   //                                 StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.ConverteDocs", MsgErro);
   //                             }
   //                             colDocs = null;

   //                         }


   //                         if (objDocVendaDest.Linhas.NumItens > 0)
   //                         {

   //                             //PREENCHE O CABECALHO DO NOVO OBJECTO
   //                             //PreencheCabecConv(objDocVendaDest, ReflectionHelper.GetPrimitiveValue<string>(TipoDocDestino.Item(i)), ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i)), NumDocProximo, DataProxima);

   //                             PreencheDadosMoeda(objDocVendaDest);

   //                             //BID 576844
   //                             string tempParam27 = "PendentePorLinha";
   //                             if ((((AgruparObjectos) ? -1 : 0) & ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam27).Valor)) != 0)
   //                             { //PriGlobal: IGNORE
   //                                 CalculaValoresTotais(ref objDocVendaDest);
   //                             }

   //                             // Preencha a data do movimento de stock igual à data do documento
   //                             int tempForVar = objDocVendaDest.Linhas.NumItens;
   //                             for (int NumLinha = 1; NumLinha <= tempForVar; NumLinha++)
   //                             {

   //                                 objDocVendaDest.Linhas.GetEdita(NumLinha).DataStock = DateTime.Parse(DateTimeHelper.ToString(objDocVendaDest.DataDoc) + " " + StringsHelper.Format(DateTimeHelper.Time, "hh:mm am/pm"));

   //                                 if (objDocVendaDest.Linhas.GetEdita(NumLinha).TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario)
   //                                 {

   //                                     //BID 595891 : no caso das Encomendas, a data de entrega deve ser sempre preenchida
   //                                     //UPGRADE_WARNING: (6021) Casting 'Variant' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
   //                                     string tempParam29 = "TipoDocumento";
   //                                     string tempParam30 = "TipoDocumento";
   //                                     string tempParam28 = "TipoDocumento";
   //                                     if (((BasBETipos.LOGTipoDocumento)ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam28).Valor)) == BasBETipos.LOGTipoDocumento.LOGDocEncomenda)
   //                                     {
   //                                         objDocVendaDest.Linhas.GetEdita(NumLinha).DataEntrega = objDocVendaDest.DataDoc.AddDays(ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(objDocVendaDest.Linhas.GetEdita(NumLinha).Artigo, "PrazoEntrega")));
   //                                         // US 28375
   //                                     }
   //                                     else if (!(((BasBETipos.LOGTipoDocumento)ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocDest.GetItem(ref tempParam29).Valor)) == BasBETipos.LOGTipoDocumento.LOGDocFinanceiro && ((BasBETipos.LOGTipoDocumento)ReflectionHelper.GetPrimitiveValue<int>(objCamposTipoDocOrig.GetItem(ref tempParam30).Valor)) == BasBETipos.LOGTipoDocumento.LOGDocStk_Transporte))
   //                                     {  //PriGlobal: IGNORE
   //                                         objDocVendaDest.Linhas.GetEdita(NumLinha).DataEntrega = DateTime.FromOADate(0);
   //                                     }
   //                                     else
   //                                     {
   //                                         string tempParam31 = "BensCirculacao";
   //                                         if (!m_objErpBSO.DSO.Plat.Utils.FBool(objCamposTipoDocOrig.GetItem(ref tempParam31)))
   //                                         { //PriGlobal: IGNORE
   //                                             objDocVendaDest.Linhas.GetEdita(NumLinha).DataEntrega = DateTime.FromOADate(0);
   //                                         }
   //                                     }

   //                                     //BID 599614
   //                                     int tempParam32 = objDocVendaDest.DataDoc.Year;
   //                                     FuncoesComuns100.FuncoesBS.ProcessosExec.InjectaContasOrc(ConstantesPrimavera100.Modulos.Vendas, objDocVendaDest.Linhas.GetEdita(NumLinha).Artigo, objDocVendaDest.Tipodoc, ref tempParam32, false, objDocVendaDest.Linhas.GetEdita(NumLinha), m_objErpBSO);

   //                                     //Nas conversões de documentos de venda, se existe reserva, vai ler os números de série ao documento de compra que deu entrada do stock
   //                                     PreencheNumerosSerieOrigemReserva(objDocVendaDest.Linhas.GetEdita(NumLinha));

   //                                 }

   //                             }

   //                             //ADICIONA O NOVO OBJECTO
   //                             objDocVendaDest.EmModoEdicao = false;
   //                             objDocVendaDest.Versao = ConstantesPrimavera100.Documentos.VersaoActual; //CS.3693
   //                             bool tempParam33 = true;
   //                             objDocVendaDest.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempParam33);
   //                             objDocVendaDest.IDEstorno = ""; //BID 563819
   //                             objDocVendaDest.IDDocB2B = ""; //BID 576341
   //                             objDocVendaDest.ATDocCodeID = ""; //BID 587763
   //                                                               //UPGRADE_WARNING: (1068) strSerieDestino() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
   //                             objDocVendaDest.Serie = ""; // ReflectionHelper.GetPrimitiveValue<string>(strSerieDestino.Item(i));
   //                             objDocVendaDest.EstadoBE = ""; //se é novo então regista como inserção no Log
   //                             objDocVendaDest.CalculoManual = false;

   //                             string tempParam34 = "LigaCC";
   //                             if (ReflectionHelper.GetPrimitiveValue<bool>(objCamposTipoDocDest.GetItem(ref tempParam34).Valor))
   //                             { //PriGlobal: IGNORE
   //                                 objDocVendaDest.Retencoes = CalculaRetencoes(objDocVendaDest);
   //                             }

   //                             string tempParam35 = "PendentePorLinha";
   //                             objDocVendaDest.GeraPendentePorLinha = ReflectionHelper.GetPrimitiveValue<bool>(objCamposTipoDocDest.GetItem(ref tempParam35).Valor); //PriGlobal: IGNORE

   //                             //BID 541522
   //                             blnErroNoActualiza = true;

   //                             if (m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.DSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, objDocVendaDest.Tipodoc, objDocVendaDest.Serie, "Origem")) >= ((short)BasBETiposGcp.EnumSerieOrigem.RecuperacaoOuManual))
   //                             {

   //                                 objDocVendaDest.RefTipoDocOrig = ConstantesPrimavera100.Documentos.RefDocOriginal_ND;
   //                                 objDocVendaDest.RefSerieDocOrig = ConstantesPrimavera100.Documentos.RefDocOriginal_ND;
   //                                 objDocVendaDest.RefSerieDocOrig = ConstantesPrimavera100.Documentos.RefDocOriginal_ND;

   //                             }

   //                             Actualiza(objDocVendaDest, MsgErro);

   //                             strID = objDocVendaDest.ID; //BID 583409

   //                             //BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc]
   //                             DocumentosGerados = DocumentosGerados + objDocVendaDest.Tipodoc + " " + objDocVendaDest.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + objDocVendaDest.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(objDocVendaDest.DataDoc) + Environment.NewLine + strDocConsiderados;
   //                             blnErroNoActualiza = false;


   //                             DocsGerados.Tipodoc = objDocVendaDest.Tipodoc;
   //                             DocsGerados.NumDoc = objDocVendaDest.NumDoc;
   //                             DocsGerados.Serie = objDocVendaDest.Serie;
   //                             DocsGerados.Filail = objDocVendaDest.Filial;


   //                             colDocumentosGerados.Add(Guid.NewGuid().ToString(), DocsGerados);

   //                             NumDocProximo = objDocVendaDest.NumDoc + 1;

   //                             objDocVendaDest = null;

   //                         }

   //                         i++;

   //                     }
			//			else
			//			{
			//				//DocsVenda.Item(i).IdDoc = tempParam3;

			//				StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.ConverteDocs", MsgErro);

			//			}

			//			m_objErpBSO.TerminaTransaccao();
			//			blnIniciouTrans = false;

			//			m_objErpBSO.Internos.Documentos.ATComunicaDocumentoId(strID, ConstantesPrimavera100.Modulos.Vendas, MsgResult); //BID 583409

			//		}

			//		if (Strings.Len(MsgResult) > 0)
			//		{
			//			strMsg = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9834, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + MsgResult;
			//		}

			//		if (Strings.Len(DocumentosGerados) > 0)
			//		{

			//			if (Strings.Len(strMsg) > 0)
			//			{
			//				strMsg = strMsg + Environment.NewLine + Environment.NewLine;
			//			}

			//			if (Strings.Len(strDocConsiderados) > 0)
			//			{

			//				//BID 547498
			//				if ((DocumentosGerados.IndexOf(m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9837, FuncoesComuns100.InterfaceComunsUS.ModuloGCP)) + 1) == 0)
			//				{
			//					strMsg = strMsg + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9837, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
			//				}

			//				strMsg = strMsg + DocumentosGerados;

			//			}
			//			else
			//			{

			//				strMsg = strMsg + DocumentosGerados;

			//			}

			//		}

			//		if (Strings.Len(strMsg) == 0)
			//		{
			//			strMsg = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15773, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
			//		}

			//		DocumentosGerados = strMsg;

			//	}
			//	else
			//	{
			//		StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.ConverteDocs", MsgErro);

			//	}

			//	colIDLinhas = null;
			//	m_clsLotesAuto = null;
			//}
			//catch (System.Exception excep)
			//{

			//	if (blnIniciouTrans)
			//	{
			//		m_objErpBSO.DesfazTransaccao();
			//	}

			//	colIDLinhas = null;
			//	m_clsLotesAuto = null;
			//	objCampos = null;

			//	MsgErro = excep.Message;

			//	if (blnErroNoActualiza)
			//	{

			//		if (Strings.Len(MsgErro) > 0)
			//		{

			//			if (MsgErro.IndexOf(Environment.NewLine) >= 0)
			//			{

			//				//BID:551238
			//				if ((MsgErro.IndexOf(Environment.NewLine) + 1) == 1)
			//				{

			//					MsgErro = MsgErro.Substring(Math.Max(MsgErro.Length - (Strings.Len(MsgErro) - 2), 0));

			//				}
			//				else
			//				{

			//					MsgErro = MsgErro.Substring(0, Math.Min(MsgErro.LastIndexOf(Environment.NewLine) + 1 - 1, MsgErro.Length));

			//				}

			//			}

			//			MsgErro = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(3067, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + " [" + MsgErro + "] " + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15213, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + strDocConsiderados + Environment.NewLine;

			//		}

			//	}

			//	//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			//	StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ConverteDocs", MsgErro);
			//}

		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk,  System.DateTime DataEmissao,  bool IncluiComentarios,  OrderedDictionary colIdProjectos,  OrderedDictionary colDocumentosGerados)
		{
			ConverteDocs( DocsVenda,  TipoDocDestino,  strSerieDestino,  AgruparObjectos,  strCamposAgrupamento,  LinhaSeparadora,  LinhaBranco,  DocumentosGerados,  AccaoRupturaStk,  DataEmissao,  IncluiComentarios,  colIdProjectos,  colDocumentosGerados, false);
		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk,  System.DateTime DataEmissao,  bool IncluiComentarios,  OrderedDictionary colIdProjectos)
		{
			OrderedDictionary tempParam435 = null;
			ConverteDocs( DocsVenda,  TipoDocDestino,  strSerieDestino,  AgruparObjectos,  strCamposAgrupamento,  LinhaSeparadora,  LinhaBranco,  DocumentosGerados,  AccaoRupturaStk,  DataEmissao,  IncluiComentarios,  colIdProjectos,  tempParam435, false);
		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk,  System.DateTime DataEmissao,  bool IncluiComentarios)
		{
			OrderedDictionary tempParam436 = null;
			OrderedDictionary tempParam437 = null;
			ConverteDocs( DocsVenda,  TipoDocDestino,  strSerieDestino,  AgruparObjectos,  strCamposAgrupamento,  LinhaSeparadora,  LinhaBranco,  DocumentosGerados,  AccaoRupturaStk,  DataEmissao,  IncluiComentarios,  tempParam436,  tempParam437, false);
		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk,  System.DateTime DataEmissao)
		{
			bool tempParam438 = true;
			OrderedDictionary tempParam439 = null;
			OrderedDictionary tempParam440 = null;
			ConverteDocs( DocsVenda,  TipoDocDestino,  strSerieDestino,  AgruparObjectos,  strCamposAgrupamento,  LinhaSeparadora,  LinhaBranco,  DocumentosGerados,  AccaoRupturaStk,  DataEmissao,  tempParam438,  tempParam439,  tempParam440, false);
		}

		public void ConverteDocs( OrderedDictionary DocsVenda,  OrderedDictionary TipoDocDestino,  OrderedDictionary strSerieDestino,  bool AgruparObjectos,  OrderedDictionary strCamposAgrupamento,  bool LinhaSeparadora,  bool LinhaBranco,  string DocumentosGerados,  short AccaoRupturaStk)
		{
			System.DateTime tempParam441 = DateTime.Parse("12:00:00 AM");
			bool tempParam442 = true;
			OrderedDictionary tempParam443 = null;
			OrderedDictionary tempParam444 = null;
			ConverteDocs( DocsVenda,  TipoDocDestino,  strSerieDestino,  AgruparObjectos,  strCamposAgrupamento,  LinhaSeparadora,  LinhaBranco,  DocumentosGerados,  AccaoRupturaStk,  tempParam441,  tempParam442,  tempParam443,  tempParam444, false);
		}

		private bool ValidaConversaoNumero(ref string TipoDoc, string Serie, ref int Numdoc, System.DateTime DataDocAConverter, System.DateTime DataProxima, ref string StrErro)
		{
			bool result = false;
			try
			{

				result = true;

				//VERIFICA SE O NOVO DOCUMENTO NAO EXISTE NO HISTORICO

				//FIL
				if (Convert.ToBoolean(m_objErpBSO.PagamentosRecebimentos.Historico.Existe(m_objErpBSO.Base.Filiais.CodigoFilial, "V", TipoDoc, Serie, Numdoc, 1, 0)))
				{
					result = false;
					string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9839, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam2 = new dynamic[]{TipoDoc, Numdoc};
					StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2) + Environment.NewLine;
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValidaConversaoNumero", excep.Message);
			}


			return result;
		}


		private bool ValidaConversao(OrderedDictionary ColObjdocvenda, OrderedDictionary TipoDoc, OrderedDictionary Serie, int Numdoc, bool AgruparObjectos, ref string StrErro)
		{
			//-------------------------------------
			bool result = false;
			string Filial = "";
			string strIDs = "";
			StdBE100.StdBELista objLista = null;
			string strSQL = "";
			string strEntAnt = "";
			string strEnt = "";
			OrderedDictionary colSeriesDocs = null;
			OrderedDictionary colDocs = null;
			VndBE100.VndBEDocumentoVenda objDocVendaDest = null;
			bool blnExisteLiq = false;
			//-------------------------------------

			try
			{

				//FIL
				Filial = m_objErpBSO.Base.Filiais.CodigoFilial;

				result = true;

				//CR.141
				colDocs = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

				//CS.203_7.50_Alpha7
				colSeriesDocs = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

				for (int i = 1; i <= ColObjdocvenda.Count; i++)
				{
                    VndBEDocumentoVenda objDocumento = ColObjdocvenda[i - 1] as VndBEDocumentoVenda;

                    string tempRefParam = Convert.ToString(objDocumento.IdDocOrigem);
					objDocVendaDest = EditaID(tempRefParam);
                    objDocumento.IdDocOrigem = tempRefParam;

					if (!AgruparObjectos)
					{ //BID 548479
						string tempRefParam2 = ReflectionHelper.GetPrimitiveValue<string>(TipoDoc[i - 1]);
						if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaLinhasNegativas(objDocVendaDest, colDocs, ref tempRefParam2, ref StrErro, ref i, ConstantesPrimavera100.Modulos.Vendas))
						{
							result = false;
						}
					} //BID 548479

					//Se existe liquidação sobre o pendente origem e o documento destino liga a CC não permitimos a gravação
					blnExisteLiq = Convert.ToBoolean(m_objErpBSO.DSO.PagamentosRecebimentos.Liquidacoes.ExisteLiquidacao(objDocVendaDest.Filial, "V", objDocVendaDest.Tipodoc, objDocVendaDest.Serie, objDocVendaDest.NumDoc));
					string tempRefParam3 = ReflectionHelper.GetPrimitiveValue<string>(TipoDoc[i - 1]);
					string tempRefParam4 = "LigaCC";
					if (blnExisteLiq && m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.DSO.Vendas.TabVendas.DaValorAtributo(ref tempRefParam3, ref tempRefParam4)))
					{

						result = false;
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9802, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

					}
					//BID 534288 - Verifica se a serie do documento de destino existe.
					if (!m_objErpBSO.Base.Series.Existe(ConstantesPrimavera100.Modulos.Vendas, ReflectionHelper.GetPrimitiveValue<string>(TipoDoc[i - 1]), ReflectionHelper.GetPrimitiveValue<string>(Serie[i - 1])))
					{
						result = false;
						string tempRefParam5 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9809, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
						dynamic[] tempRefParam6 = new dynamic[]{Serie[i - 1], TipoDoc[i - 1]};
						StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam5, tempRefParam6) + Environment.NewLine;
					}
					//^BID 534288

					//CS.203_7.50_Alpha7 - Verifica se os tipos de lancamento são iguais para os Docs de Origem e Destino..
					string tempRefParam7 = ReflectionHelper.GetPrimitiveValue<string>(Serie[i - 1]);
					result = result && FuncoesComuns100.FuncoesBS.Documentos.ValidaTiposLancamentoConversao(ref i, ref StrErro, ConstantesPrimavera100.Modulos.Vendas, objDocVendaDest.Tipodoc, objDocVendaDest.Serie, ReflectionHelper.GetPrimitiveValue<string>(TipoDoc[i - 1]), ref tempRefParam7, colSeriesDocs);
					//END CS.203_7.50_Alpha7

					//Valida os documentos de origem. Se for para comunicar e ainda nao tem código, então não o poderá converter
					if (Strings.Len(objDocVendaDest.ATDocCodeID) == 0)
					{

						if (FuncoesComuns100.FuncoesBS.Documentos.DocumentoTransporteComunicacaoAT(objDocVendaDest, ConstantesPrimavera100.Modulos.Vendas))
						{

							result = false;
							string tempRefParam8 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(16645, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
							dynamic[] tempRefParam9 = new dynamic[]{objDocVendaDest.Tipodoc, objDocVendaDest.NumDoc, objDocVendaDest.Serie};
							StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam8, tempRefParam9) + Environment.NewLine;

						}

					}

					objDocVendaDest = null;
				}

				colDocs = null;
				//END CR.141


				//VERIFICA SE OS REGIMES DE IVA ESTAO EM CONFORMIDADE
				if (AgruparObjectos)
				{

					for (int i = 1; i <= ColObjdocvenda.Count; i++)
					{
						if (Strings.Len(strIDs) > 0)
						{
							strIDs = strIDs + ", ";
						}
						//strIDs = strIDs + "'" + Convert.ToString(ColObjdocvenda[i - 1].IdDoc) + "'";
					}

					strSQL = "SELECT DISTINCT Entidade, RegimeIva, DescEntidade, DescPag FROM CabecDoc WHERE Id IN (" + strIDs + ") ORDER BY Entidade";
					objLista = m_objErpBSO.Consulta(strSQL);

					strEntAnt = "";
					while (!objLista.NoFim())
					{
						strEnt = objLista.Valor("Entidade");

						if (Strings.Len(strEntAnt) > 0)
						{
							// Não é o primeiro registo
							if (strEntAnt == strEnt)
							{
								// Documentos com condições diferentes, para a mesma entidade
								result = false;
								string tempRefParam10 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9842, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
								dynamic[] tempRefParam11 = new dynamic[]{strEnt};
								StrErro = StrErro + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam10, tempRefParam11) + Environment.NewLine + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9844, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
							}
						}

						objLista.Seguinte();
						if (!objLista.NoFim())
						{
							strEntAnt = strEnt;
						}

					}

					objLista = null;

					//CS.203_7.50_Alpha7
					colSeriesDocs = null;
				}
			}
			catch (System.Exception excep)
			{

				//CS.203_7.50_Alpha7
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ValidaConversao", excep.Message);
			}

			return result;
		}

		private void PreencheCabecConv(VndBE100.VndBEDocumentoVenda objDocVenda, string TipoDocDestino, string strSerie, int NumDocProximo, System.DateTime DataProxima)
		{

			try
			{


				//FIL
				objDocVenda.Filial = m_objErpBSO.Base.Filiais.CodigoFilial;

				objDocVenda.Serie = strSerie;
				objDocVenda.Tipodoc = TipoDocDestino;
				objDocVenda.NumDoc = NumDocProximo;
				objDocVenda.Utilizador = m_objErpBSO.Contexto.UtilizadorActual;
				objDocVenda.Posto = m_objErpBSO.Vendas.CaixaPostos.DaCaixaPosto();
				objDocVenda.DataUltimaActualizacao = DateTime.Now;
				System.DateTime tempRefParam = objDocVenda.DataDoc;
				string tempRefParam2 = objDocVenda.CondPag;
				int tempRefParam3 = 0;
				string tempRefParam4 = objDocVenda.TipoEntidade;
				string tempRefParam5 = objDocVenda.Entidade;
				objDocVenda.DataVenc = CalculaDataVencimento(tempRefParam, tempRefParam2, tempRefParam3, tempRefParam4, tempRefParam5);
				objDocVenda.RegimeIva = objDocVenda.RegimeIva;
				objDocVenda.DocImpresso = false;

				objDocVenda.IDCabecMovCbl = "";
				objDocVenda.CBLEstado = 0;
				objDocVenda.CBLDiario = "";
				objDocVenda.CBLNumDiario = "0";
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheCabecConv", excep.Message);
			}


		}

		private void CriaRelacaoIDs(OrderedDictionary colIDLinhas, string strAntigoID, string strNovoID)
		{
			string[] arrIDs = new string[]{"", ""};

			arrIDs[0] = strAntigoID;
			arrIDs[1] = strNovoID;

			colIDLinhas.Add(Guid.NewGuid().ToString(), arrIDs);
		}

		private string DaNovoID(OrderedDictionary colIDLinhas, string strAntigoID)
		{

			string result = "";
			result = "";

			for (int lngPos = 1; lngPos <= colIDLinhas.Count; lngPos++)
			{
				if (ReflectionHelper.GetPrimitiveValue<string>(((Array) colIDLinhas[lngPos - 1]).GetValue(0)) == strAntigoID)
				{
					result = ReflectionHelper.GetPrimitiveValue<string>(((Array) colIDLinhas[lngPos - 1]).GetValue(1));
					break;
				}
			}
			return result;
		}

		//AccaoRupturaStk:
		//       0 - Não converte o documento
		//       1 - Converte as linhas que não estão em ruptura
		//       2 - Ignora
		private void PreencheLinhasConv(VndBE100.VndBEDocumentoVenda objDocVenda, VndBE100.VndBELinhasDocumentoVenda objLinhas, bool LinhaSeparadora, bool LinhaBranco, bool UpdateLinhas, string Descricao, bool MovStk, int AccaoRupturaStk, ref string strMsgResult, ref string DocsConvertidos, System.DateTime DataDoc, bool IncluiComentarios, OrderedDictionary colIDLinhas, OrderedDictionary colIdProjectos, bool LotesAutomaticos, ref string TipoDocDestino)
		{

			VndBE100.VndBELinhaDocumentoVenda ObjLinha = null;
			VndBE100.VndBELinhasDocumentoVenda objLinhasOrig = null;
			VndBE100.VndBELinhasDocumentoVenda objLinhasTransf = null;
			BasBELinhaHistoricoResiduo ObjResiduoLin = null;
			string strMsg = "";
			bool blnConverteDoc = false;
			bool blnSoComentarios = false;
			string strNovoID = "";
			int lngNumLinhaPai = 0;
			bool blnConverteuFilhos = false;
			string strEstadoOrigem = "";
			string strEstadoDestino = "";
			InvBE100.InvBETipos.EnumTipoConfigEstados intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;
			bool blnMovInvertido = false;

			StdBE100.StdBECampos objCampos = null;
			bool blnReservaAuto = false;
			bool blnReservasParciais = false;
			string strEstadoReserva = "";
			dynamic objReservasPend = null;
			string strIdTipoOrigemDestino = "";
			dynamic objReserva = null;
			double dblQtReservadaAuto = 0;
			BasBETipos.LOGTipoDocumento intTipoDocDestino = BasBETipos.LOGTipoDocumento.LOGDocPedidoCotacao;

			try
			{

				blnSoComentarios = true;
				blnConverteDoc = true;

				objLinhasTransf = new VndBE100.VndBELinhasDocumentoVenda();

				int tempForVar = objLinhas.NumItens;
				for (int lngIndice = 1; lngIndice <= tempForVar; lngIndice++)
				{

					//Estados do inventário
					if (Strings.Len(objLinhas.GetEdita(lngIndice).Artigo) > 0)
					{

						//objLinhas.GetEdita(lngIndice).ReservaStock = new dynamic();
						string tempRefParam = objDocVenda.Tipodoc;
						if (!FuncoesComuns100.FuncoesBS.Documentos.LinhaMovimentaStocksTransformacao(objLinhas.GetEdita(lngIndice).IdLinha, ConstantesPrimavera100.Modulos.Vendas, ref tempRefParam, ref TipoDocDestino, ref blnMovInvertido))
						{

							objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = "";
							objLinhas.GetEdita(lngIndice).INV_EstadoDestino = "";

						}
						else
						{

							//Assume a data do documento de destino, uma vez que se não estiver definida neste ponto vai dar erro de inventário
							objLinhas.GetEdita(lngIndice).DataStock = DateTime.Parse(DateTimeHelper.ToString(objDocVenda.DataDoc) + " " + StringsHelper.Format(DateTimeHelper.Time, "hh:mm am/pm"));

							if (objLinhas.GetEdita(lngIndice).Quantidade >= 0)
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

							}
							else
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovNegativos;

							}

							//Se o estado destino do movimento origem está preenchido
							if (Strings.Len(objLinhas.GetEdita(lngIndice).INV_EstadoDestino) != 0)
							{

								//Se os estados origem e destino estão preenchidos, o estado origem do movimento destino
								//passa a ser o destino do anterior e o destino é o estado default
								if (Strings.Len(objLinhas.GetEdita(lngIndice).INV_EstadoOrigem) != 0)
								{

									m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
									objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = objLinhas.GetEdita(lngIndice).INV_EstadoDestino;
									objLinhas.GetEdita(lngIndice).INV_EstadoDestino = strEstadoDestino;

								}
								else
								{

									//Se o estado destino conta para existencias não preenche os estados, ou seja não movimenta
									if (!blnMovInvertido && m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Inventario.EstadosInventario.DaValorAtributo(objLinhas.GetEdita(lngIndice).INV_EstadoDestino, "Existencias")))
									{

										objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = "";
										objLinhas.GetEdita(lngIndice).INV_EstadoDestino = "";

									}
									else
									{

										//Se o estado destino não conta para existencias, o estado origem do movimento destino
										//passa a ser o destino do anterior e o destino é o estado default
										//Obter os estados default
										m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
										objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = objLinhas.GetEdita(lngIndice).INV_EstadoDestino;
										objLinhas.GetEdita(lngIndice).INV_EstadoDestino = strEstadoDestino;

									}

								}

							}
							else
							{

								if (!blnMovInvertido && Strings.Len(objLinhas.GetEdita(lngIndice).INV_EstadoOrigem) != 0)
								{

									objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = "";
									objLinhas.GetEdita(lngIndice).INV_EstadoDestino = "";

								}
								else
								{

									//Caso contrário vou usar os estados default configurados para o documento
									//Obter os estados default
									m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), TipoDocDestino, intTipoMov, strEstadoOrigem, strEstadoDestino);
									objLinhas.GetEdita(lngIndice).INV_EstadoOrigem = strEstadoOrigem;
									objLinhas.GetEdita(lngIndice).INV_EstadoDestino = strEstadoDestino;

								}

							}

						}

					}

				}

				//Reservas automaticas - INICIO
				dynamic[] tempRefParam2 = new dynamic[]{"ReservaAutomatica", "ReservasParciais", "EstadoReserva", "TipoDocumento"};
				objCampos = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(TipoDocDestino, tempRefParam2);

				string tempRefParam3 = "ReservaAutomatica";
				blnReservaAuto = m_objErpBSO.DSO.Plat.Utils.FBool(objCampos.GetItem(ref tempRefParam3));
				string tempRefParam4 = "ReservasParciais";
				blnReservasParciais = m_objErpBSO.DSO.Plat.Utils.FBool(objCampos.GetItem(ref tempRefParam4));
				string tempRefParam5 = "EstadoReserva";
				strEstadoReserva = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam5));
				//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				string tempRefParam6 = "TipoDocumento";
				intTipoDocDestino = (BasBETipos.LOGTipoDocumento) m_objErpBSO.DSO.Plat.Utils.FInt(objCampos.GetItem(ref tempRefParam6));

				if (blnReservaAuto)
				{

					strIdTipoOrigemDestino = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));

					//objReservasPend = new InvBE100.dynamic();

					foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha2 in objLinhas)
					{
						ObjLinha = ObjLinha2;

						string tempRefParam7 = objDocVenda.ID;
						objReserva = m_objErpBSO.Vendas.Documentos.SugereReservasAutomaticas(ObjLinha.Artigo, ObjLinha.Quantidade, strIdTipoOrigemDestino, ObjLinha.IdLinha, "", strEstadoReserva, objReservasPend, ObjLinha.Armazem, ObjLinha.Localizacao, ObjLinha.Lote, tempRefParam7);
						dblQtReservadaAuto = DaTotalReservado(objReserva.Linhas);

						if (blnReservasParciais || (dblQtReservadaAuto == ObjLinha.Quantidade))
						{

							ObjLinha.ReservaStock = objReserva;

						}
						else
						{

							//ObjLinha.ReservaStock = new dynamic();

						}

						objReserva = null;
						ObjLinha = null;
						ObjLinha = null;
					}


					objReservasPend = null;

				}

				objCampos = null;

				//Reservas automaticas - FIM


				if (UpdateLinhas)
				{

					if (colIdProjectos == null)
					{

						objLinhasOrig = objLinhas;

					}
					else
					{

						objLinhasOrig = new VndBE100.VndBELinhasDocumentoVenda();

						int tempForVar2 = objLinhas.NumItens;
						for (int lngIndice = 1; lngIndice <= tempForVar2; lngIndice++)
						{

							if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objLinhas.GetEdita(lngIndice).IDObra, colIdProjectos))
							{

								objLinhasOrig.Insere(objLinhas.GetEdita(lngIndice));

							}

						}

					}

				}
				else
				{

					objLinhasOrig = new VndBE100.VndBELinhasDocumentoVenda();

					int tempForVar3 = objDocVenda.Linhas.NumItens;
					for (int lngIndice = 1; lngIndice <= tempForVar3; lngIndice++)
					{

						if (colIdProjectos == null)
						{

							objLinhasOrig.Insere(objDocVenda.Linhas.GetEdita(lngIndice));

						}
						else if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objDocVenda.Linhas.GetEdita(lngIndice).IDObra, colIdProjectos))
						{ 

							objLinhasOrig.Insere(objDocVenda.Linhas.GetEdita(lngIndice));

						}

					}

					objDocVenda.Linhas = new VndBE100.VndBELinhasDocumentoVenda();

				}

				//Para documentos Stock/Trans e Faturas, desdobra as reservas efetuadas
				if (intTipoDocDestino >= BasBETipos.LOGTipoDocumento.LOGDocStk_Transporte)
				{

					DesdobraLinhasReservadas(objLinhasOrig);

				}

				//US 27510
				PreencheLinhasTransf(ref objLinhasOrig, objLinhasTransf, MovStk, AccaoRupturaStk, ref strMsg, IncluiComentarios, LotesAutomaticos, ref blnConverteDoc, ref lngNumLinhaPai, ref blnConverteuFilhos, ref blnSoComentarios);

				if (lngNumLinhaPai != 0 && !blnConverteuFilhos)
				{

					objLinhasTransf.Remove(lngNumLinhaPai); //BID 524364

				}

				if (blnConverteDoc)
				{

					if (Strings.Len(strMsg) > 0)
					{

						if (AccaoRupturaStk == 2)
						{

							strMsg = Descricao + Environment.NewLine + strMsg;

						}
						else
						{

							strMsg = Descricao + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9848, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine + strMsg;

						}

						strMsg = strMsg + Environment.NewLine;

					}

					if (objLinhasTransf.NumItens > 0 && !blnSoComentarios)
					{

						// Se a data do documento original for superior à do documento resultante, actualiza
						if (DataDoc > objDocVenda.DataDoc)
						{
							objDocVenda.DataDoc = DataDoc;
						}

						// String com indicação dos documentos convertidos
						DocsConvertidos = DocsConvertidos + "\t" + "- " + Descricao + Environment.NewLine;

						// Indicação do documento origem, colocada no documento convertido
						if (LinhaSeparadora)
						{

							if (LinhaBranco)
							{
								InsereLinhaBranco(objDocVenda);
							}

							InsereLinhaBranco(objDocVenda, Descricao);

							if (LinhaBranco)
							{
								InsereLinhaBranco(objDocVenda);
							}

						}

						foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha3 in objLinhasTransf)
						{
							ObjLinha = ObjLinha3;


							bool tempRefParam8 = true;
							strNovoID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam8);
							CriaRelacaoIDs(colIDLinhas, ObjLinha.IdLinha, strNovoID);

							if (ObjLinha.TipoLinha != "60")
							{

								ObjLinha.IDLinhaOriginal = ObjLinha.IdLinha;
								ObjLinha.IdLinha = strNovoID;
								ObjLinha.ModuloOrigemCopia = "";
								ObjLinha.IdLinhaOrigemCopia = "";

								if (Strings.Len(ObjLinha.IdLinhaPai) > 0)
								{
									ObjLinha.IdLinhaPai = DaNovoID(colIDLinhas, ObjLinha.IdLinhaPai);
								}

								if (MovStk)
								{

									ObjLinha.PCM = Convert.ToDouble(m_objErpBSO.Inventario.Custeio.DaCusto(ObjLinha.Artigo, ObjLinha.Armazem, null, ObjLinha.Lote, ObjLinha.DataStock, ObjLinha.Quantidade));

								}

							}
							else
							{

								ObjLinha.IdLinha = strNovoID;

							}

							foreach (BasBELinhaHistoricoResiduo ObjResiduoLin2 in ObjLinha.LinhasHistoricoResiduo)
							{
								ObjResiduoLin = ObjResiduoLin2;

								bool tempRefParam9 = true;
								ObjResiduoLin.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam9);
								ObjResiduoLin.IdLinha = ObjLinha.IdLinha;

								ObjResiduoLin = null;
							}



							ObjLinha.EstadoBD = BasBETiposGcp.enuEstadosBD.estNovo;

							objDocVenda.Linhas.Insere(ObjLinha);

							ObjLinha = null;
						}


					}

				}
				else
				{

					//BID 593086 : a descrição deve estar no formato [TipoDoc Serie/NumDoc]
					strMsg = objDocVenda.Tipodoc + " " + objDocVenda.Serie + "/" + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13309, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + objDocVenda.NumDoc.ToString() + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(4077, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + DateTimeHelper.ToString(objDocVenda.DataDoc) + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9852, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + 
					         Environment.NewLine + 
					         strMsg;
					//& _
					//'vbTab & m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9854, FuncoesComuns100.ModuloGCP)

				}

				strMsgResult = strMsgResult + strMsg;

				ObjLinha = null;
				objLinhasOrig = null;
				objLinhasTransf = null;
				ObjResiduoLin = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheLinhasConv", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : DesdobraLinhasReservadas
		// Description : Faz o desdobramento conforme as linhas que tem reservas
		// Arguments   : Linhas -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void DesdobraLinhasReservadas(VndBE100.VndBELinhasDocumentoVenda Linhas)
		{
			dynamic objReservas = null;
			VndBE100.VndBELinhaDocumentoVenda objLinhaDoc = null;
			OrderedDictionary colQuantReservadas = null;
			OrderedDictionary colLinhasNovas = null;
			VndBE100.VndBELinhaDocumentoVenda objLinhaNova = null;
			dynamic objLinhaReserva = null;
			double dblQuantPendente = 0;
			double dblQuantEntregar = 0;
			string strIdTipoOrigem = "";
			GcpNumerosSerie objNumsSerie = null;
			BasBENumeroSerie objBENumSerie = null;


			//Le as reservas da Venda
			try
			{

				colQuantReservadas = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
				colLinhasNovas = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);

				//Percorre todas as linhas
				foreach (VndBE100.VndBELinhaDocumentoVenda objLinhaDoc2 in Linhas)
				{
					objLinhaDoc = objLinhaDoc2;

					//Apenas para linhas abertas e que não sejam novas criadas por este processo
					if (objLinhaDoc.TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario && !objLinhaDoc.Fechado && !FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objLinhaDoc.IdLinha, colLinhasNovas))
					{

						strIdTipoOrigem = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));
						objReservas = (dynamic) m_objErpBSO.Inventario.Reservas.EditaDestino(strIdTipoOrigem, objLinhaDoc.IdLinha);

						//Inicia a quant pendente
						dblQuantPendente = (objLinhaDoc.Quantidade * objLinhaDoc.FactorConv) - (objLinhaDoc.QuantSatisfeita * objLinhaDoc.FactorConv);

						//Se tem reservas...
						if (objReservas.Linhas.NumItens > 0 && dblQuantPendente != 0)
						{

							//A reserva tem registos? Divide a quantidade seleccionada pelas reservas
							foreach (dynamic objLinhaReserva2 in objReservas)
							{
								objLinhaReserva = objLinhaReserva2;

								//Apenas para reservas se ainda não tenham sido tratadas
								if (String.Compare(objLinhaReserva.ID, objLinhaDoc.INV_IDReserva, true) != 0)
								{

									//Se a reserva não está fechada e ainda tem unidades pendentes
									if (!objLinhaReserva.Fechada && objLinhaReserva.QuantidadePendente > 0 && m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Inventario.EstadosInventario.DaValorAtributo(objLinhaReserva.EstadoDestino, "Existencias")))
									{

										//Decrementa a quantidade já convertida
										if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objLinhaReserva.ID, colQuantReservadas))
										{

											objLinhaReserva.QuantidadePendente -= ((double) colQuantReservadas[objLinhaReserva.ID]);

										}

										if (objLinhaReserva.QuantidadePendente != 0)
										{

											//Subtrai a quantidade ainda pendente, senão usa a quantidade pendnte para transformar
											if ((dblQuantPendente - objLinhaReserva.QuantidadePendente) >= 0)
											{

												dblQuantPendente -= objLinhaReserva.QuantidadePendente;
												dblQuantEntregar = objLinhaReserva.QuantidadePendente;

											}
											else
											{
												dblQuantEntregar = dblQuantPendente;
												dblQuantPendente = 0;
											}

											//Quando ainda temos quantidade pendente, cria-se uma nova linha
											if (dblQuantPendente != 0)
											{

												//Clonar a linha actual para uma nova
												objLinhaNova = (VndBE100.VndBELinhaDocumentoVenda) m_objErpBSO.DSO.Plat.FuncoesGlobais.ClonaObjecto(objLinhaDoc, false, null);

												//Acrescenta à colecção de novas linhas e à lista de linhas
												if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objLinhaNova.IdLinha, colLinhasNovas))
												{

													colLinhasNovas.Add(objLinhaNova.IdLinha, objLinhaNova);

												}

												Linhas.Insere(objLinhaNova);

												//Senão, usa a própria linha
											}
											else
											{

												objLinhaNova = objLinhaDoc;

											}

											//Aplica novamente o factor de conversão
											dblQuantEntregar /= objLinhaDoc.FactorConv;

											//Novos dados da linha
											objLinhaNova.Quantidade = dblQuantEntregar;
											objLinhaNova.QuantSatisfeita = 0;
											objLinhaNova.Armazem = objLinhaReserva.Armazem;
											objLinhaNova.Localizacao = objLinhaReserva.Localizacao;
											objLinhaNova.Lote = objLinhaReserva.Lote;
											objLinhaNova.INV_IDReserva = objLinhaReserva.ID;

											objNumsSerie = new GcpNumerosSerie();
											//Obter os números de série movimentados na reserva
											DaNumerosSerieTransformacaoReserva(objLinhaNova, objNumsSerie);

											foreach (GcpNumeroSerie objNSerie in objNumsSerie)
											{

												objBENumSerie = new BasBENumeroSerie();
												objBENumSerie.Modulo = ConstantesPrimavera100.Modulos.Compras;
												objBENumSerie.NumeroSerie = objNSerie.NumeroSerie;
												objBENumSerie.IdNumeroSerie = objNSerie.IdNumeroSerie;

												objLinhaNova.NumerosSerie.Insere(objBENumSerie);

											}

											objNumsSerie = null;

											//Acertar os números de série, apagamos os primeiros
											if (objLinhaNova.NumerosSerie.NumItens > objLinhaNova.Quantidade)
											{

												for (int lngNumSerie = 1; lngNumSerie <= objLinhaNova.NumerosSerie.NumItens - objLinhaNova.Quantidade; lngNumSerie++)
												{

													objLinhaNova.NumerosSerie.Remove(1);

												}

											}

											objLinhaNova = null;

											if (dblQuantPendente == 0)
											{

												break;

											}

										}

									}
								}
								else
								{

									objLinhaDoc.Armazem = objLinhaReserva.Armazem;
									objLinhaDoc.Localizacao = objLinhaReserva.Localizacao;
									objLinhaDoc.Lote = objLinhaReserva.Lote;


								}

								objLinhaReserva = null;
							}
							 //For Each objLinhaReserva

							//Se ainda ficou com quant pendente, actualiza na linha
							if (dblQuantPendente != 0)
							{

								objLinhaDoc.Quantidade = dblQuantPendente / objLinhaDoc.FactorConv;
								objLinhaDoc.QuantSatisfeita = 0;

								//Acertar os números de série, apagamos os últimos
								if (objLinhaDoc.NumerosSerie.NumItens > objLinhaDoc.Quantidade)
								{

									for (int lngNumSerie = objLinhaDoc.NumerosSerie.NumItens; lngNumSerie >= objLinhaDoc.Quantidade + 1; lngNumSerie--)
									{

										objLinhaDoc.NumerosSerie.Remove(objLinhaDoc.NumerosSerie.NumItens);

									}

								}

							}

							objReservas = null;

						}

					}

					objLinhaDoc = null;
				}
				 //For Each objLinhaDoc

				objReservas = null;
				objLinhaDoc = null;
				colQuantReservadas = null;
				colLinhasNovas = null;
				objLinhaNova = null;
				objLinhaReserva = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.InsereLinhaBranco", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure     : PreencheLinhasTransf
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		private void PreencheLinhasTransf(ref VndBE100.VndBELinhasDocumentoVenda LinhasOrig, VndBE100.VndBELinhasDocumentoVenda LinhasTransf, bool MovStk, int AccaoRupturaStk, ref string Msg, bool IncluiComentarios, bool LotesAutomaticos, ref bool ConverteDoc, ref int NumLinhaPai, ref bool ConverteuFilhos, ref bool SoComentarios)
		{

			StdBE100.StdBECampos objCamposArt = null;
			double dblQuantPend = 0;
			bool blnArtMovStock = false;
			string strIdLinhaPai = "";
			double dblStkArtArm = 0;
			bool blnTratamentoLotes = false;

			try
			{

				//US 27510: Consumo automático de lotes
				if (MovStk)
				{

					if (LotesAutomaticos)
					{


						m_clsLotesAuto.Processa(ref LinhasOrig);

						if (Strings.Len(m_clsLotesAuto.Msg) > 0)
						{

							Msg = Msg + m_clsLotesAuto.Msg;

						}


					}

				}
				//^US 27510

				int tempForVar = LinhasOrig.NumItens;
				for (int lngLinha = 1; lngLinha <= tempForVar; lngLinha++)
				{

					if (!LinhasOrig.GetEdita(1).Fechado)
					{

						//CR.141 - Foram retirados os ABS's
						dblQuantPend = LinhasOrig.GetEdita(1).Quantidade - LinhasOrig.GetEdita(1).QuantSatisfeita;

						if (LinhasOrig.GetEdita(1).TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentario)
						{

							if (dblQuantPend != 0)
							{

								objCamposArt = m_objErpBSO.Base.Artigos.DaValorAtributos(LinhasOrig.GetEdita(1).Artigo, "MovStock", "TratamentoLotes");

								if (objCamposArt != null)
								{

									//Verifica se o artigo movimenta stock
									string tempRefParam = "MovStock";
									blnArtMovStock = (m_objErpBSO.DSO.Plat.Utils.FStr(objCamposArt.GetItem(ref tempRefParam)) == "S"); //PriGlobal: IGNORE

									//Verifica se o artigo faz gestão de lotes
									string tempRefParam2 = "TratamentoLotes";
									blnTratamentoLotes = m_objErpBSO.DSO.Plat.Utils.FBool(objCamposArt.GetItem(ref tempRefParam2)); //PriGlobal: IGNORE

									objCamposArt = null;

								}

								//So faz se a linha não tem uma reserva associada, senão está a usar essa mesma reserva
								if (MovStk && AccaoRupturaStk != 2 && blnArtMovStock && Strings.Len(LinhasOrig.GetEdita(1).INV_IDReserva) == 0)
								{

									if (LinhasOrig.GetEdita(1).IdLinhaPai != strIdLinhaPai)
									{

										if (NumLinhaPai != 0 && !ConverteuFilhos)
										{
											LinhasTransf.Remove(NumLinhaPai);
										}

										strIdLinhaPai = "";
										NumLinhaPai = 0;

									}

									dblStkArtArm = Convert.ToDouble(m_objErpBSO.Inventario.Stocks.DaStockArtigo(LinhasOrig.GetEdita(1).Artigo, LinhasOrig.GetEdita(1).DataStock, LinhasOrig.GetEdita(1).Armazem, LinhasOrig.GetEdita(1).Localizacao, LinhasOrig.GetEdita(1).Lote, null, InvBE100.InvBETipos.FlagFiltroEstado.Sim, null, null, InvBE100.InvBETipos.FlagFiltroEstado.Nao, InvBE100.InvBETipos.FlagFiltroEstado.Nao));

									//comentado: A quantidade reservada é tratada pelas reservas
									//dblStkArtArm = dblStkArtArm + (LinhasOrig(1).QuantReservada * LinhasOrig(1).FactorConv) 'BID 550042

									if ((dblStkArtArm < dblQuantPend * LinhasOrig.GetEdita(1).FactorConv) && (LinhasOrig.GetEdita(1).TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo))
									{

										if (Strings.Len(Msg) > 0)
										{
											Msg = Msg + Environment.NewLine;
										}

										string tempRefParam3 = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9846, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
										dynamic[] tempRefParam4 = new dynamic[]{LinhasOrig.GetEdita(1).Artigo, LinhasOrig.GetEdita(1).Armazem};
										Msg = Msg + "\t" + m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam3, tempRefParam4);

										if (AccaoRupturaStk == 0)
										{
											ConverteDoc = false;
										}

										if (dblStkArtArm > 0)
										{ //BID 578333

											if (LinhasOrig.GetEdita(1).TipoLinha != ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo && LinhasOrig.GetEdita(1).IdLinhaPai == strIdLinhaPai && Strings.Len(strIdLinhaPai) > 0)
											{
												LinhasTransf.GetEdita(NumLinhaPai).Quantidade -= dblQuantPend;
											}

											LinhasOrig.GetEdita(1).Quantidade = dblStkArtArm;
											LinhasOrig.GetEdita(1).QuantSatisfeita = 0;
											LinhasOrig.GetEdita(1).QuantReservada = 0; //BID 592340

											LinhasTransf.Insere(LinhasOrig.GetEdita(1));

											SoComentarios = false;

										}

									}
									else
									{

										LinhasOrig.GetEdita(1).Quantidade = dblQuantPend;
										LinhasOrig.GetEdita(1).QuantSatisfeita = 0;
										LinhasOrig.GetEdita(1).QuantReservada = 0; //BID 592340

										LinhasTransf.Insere(LinhasOrig.GetEdita(1));

										SoComentarios = false;

										if (LinhasOrig.GetEdita(1).TipoLinha == ConstantesPrimavera100.Documentos.TipoLinComentarioArtigo)
										{

											strIdLinhaPai = LinhasOrig.GetEdita(1).IdLinha;
											NumLinhaPai = LinhasTransf.NumItens;
											ConverteuFilhos = false;

										}
										else if (LinhasOrig.GetEdita(1).IdLinhaPai == strIdLinhaPai)
										{ 

											ConverteuFilhos = true;

										}

									}

								}
								else
								{

									LinhasOrig.GetEdita(1).Quantidade = dblQuantPend;
									LinhasOrig.GetEdita(1).QuantSatisfeita = 0;
									LinhasOrig.GetEdita(1).QuantReservada = 0; //BID 592340

									LinhasTransf.Insere(LinhasOrig.GetEdita(1));

									SoComentarios = false;

								}

							}


						}
						else
						{
							// Passa a linha de comentario (ConstantesPrimavera100.DOCUMENTOS.TIPOLINCOMENTARIO)

							if (IncluiComentarios)
							{

								LinhasOrig.GetEdita(1).Quantidade = dblQuantPend;
								LinhasOrig.GetEdita(1).QuantSatisfeita = 0;
								LinhasOrig.GetEdita(1).QuantReservada = 0; //BID 592340

								LinhasTransf.Insere(LinhasOrig.GetEdita(1));

							}

						}

					}

					LinhasOrig.Remove(1);

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_PreencheLinhasTransf", excep.Message);
			}

		}

		private void InsereLinhaBranco(VndBE100.VndBEDocumentoVenda objDocVenda, string Descricao = "")
		{
			VndBE100.VndBELinhaDocumentoVenda ObjLinhaBranco = null;

			try
			{

				ObjLinhaBranco = new VndBE100.VndBELinhaDocumentoVenda();

				bool tempRefParam = true;
				ObjLinhaBranco.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);
				ObjLinhaBranco.Descricao = Descricao;
				ObjLinhaBranco.TipoLinha = "60";
				ObjLinhaBranco.TaxaIva = 0;
				ObjLinhaBranco.PrecUnit = 0;
				ObjLinhaBranco.Quantidade = 0;
				ObjLinhaBranco.RegimeIva = objDocVenda.RegimeIva;

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(ObjLinhaBranco.CamposUtil, DaDefCamposUtilLinhas());

				objDocVenda.Linhas.Insere(ObjLinhaBranco);
				ObjLinhaBranco = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.InsereLinhaBranco", excep.Message);
			}

		}

		public bool ExisteDimensao(string strArtigoFilho, string IdLinhaPai)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.ExisteDimensao(ref strArtigoFilho, ref IdLinhaPai);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ExisteDimensao", excep.Message);
			}
			return false;
		}

		//##SUMMARY Permite adicionar uma linha no objecto de linhas/dimensão.
		//##PARAM clsLinhaDoc Objecto com as Linhas do documento de venda.
		//##PARAM RubDim1 Identifica a primeira rubrica.
		//##PARAM RubDim2 Identifica a segunda rubrica.
		//##PARAM RubDim3 Identifica a terceira rubrica.
		//##PARAM Quantidade Identifica a quantidade a considerar.
		//##PARAM QuantReservada Identifica a quantidade Reservada a considerar.
		public VndBELinhaDocumentoVenda AdicionaLinhaDim(VndBELinhaDocumentoVenda ClsLinhaDoc, string RubDim1, string RubDim2 = "", string RubDim3 = "", double Quantidade = 1, double QuantReservada = 0)
		{

			try
			{


				return ClsLinhaDoc;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.AdicionaLinhaDim", excep.Message);
			}
			return null;
		}

		public StdBEDefCamposUtil DaDefCamposUtil()
		{
			try
			{

				if (m_objDefCamposUtil == null)
				{

					if (FuncoesComuns100.FuncoesBS.Utils.PossuiExtensibilidadeBD())
					{
						m_objDefCamposUtil = m_objErpBSO.DSO.Vendas.Documentos.DaDefCamposUtil();
					}
					else
					{
						m_objDefCamposUtil = new StdBE100.StdBEDefCamposUtil();
					}

				}


				return m_objDefCamposUtil;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaDefCamposUtil", excep.Message);
			}
			return null;
		}

		public StdBEDefCamposUtil DaDefCamposUtilLinhas()
		{
			try
			{

				if (m_objDefCamposUtilLinha == null)
				{

					if (FuncoesComuns100.FuncoesBS.Utils.PossuiExtensibilidadeBD())
					{
						m_objDefCamposUtilLinha = m_objErpBSO.DSO.Vendas.Documentos.DaDefCamposUtilLinhas();
					}
					else
					{
						m_objDefCamposUtilLinha = new StdBE100.StdBEDefCamposUtil();
					}

				}


				return m_objDefCamposUtilLinha;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaDefCamposUtil", excep.Message);
			}
			return null;
		}

		public void ProcuraDocAnteriores(string IdCabec, BasBEDocumentosRastreab ClsDocumentos)
		{

			try
			{

				m_objErpBSO.DSO.Vendas.Documentos.ProcuraDocAnteriores(ref IdCabec, ref ClsDocumentos);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ProcuraDocAnteriores", excep.Message);
			}

		}

		public void ProcuraDocPosteriores(string IdCabec, BasBEDocumentosRastreab ClsDocumentos)
		{

			try
			{

				m_objErpBSO.DSO.Vendas.Documentos.ProcuraDocPosteriores(ref IdCabec, ref ClsDocumentos);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ProcuraDocPosteriores", excep.Message);
			}

		}

		public void ProcuraLinhasAnteriores(string IdLinha, BasBELinhasRastreabilidade clsLinhas)
		{

			try
			{

				m_objErpBSO.DSO.Vendas.Documentos.ProcuraLinhasAnteriores(ref IdLinha, ref clsLinhas);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ProcuraLinhasAnteriores", excep.Message);
			}

		}

		public void ProcuraLinhasPosteriores(string IdLinha, BasBELinhasRastreabilidade clsLinhas)
		{
			try
			{

				m_objErpBSO.DSO.Vendas.Documentos.ProcuraLinhasPosteriores(ref IdLinha, ref clsLinhas);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ProcuraLinhasPosteriores", excep.Message);
			}

		}

		public double[] CalculaPrecoLinha(string TipoLinha, bool SujeitoRetencao, double PercentagemRetencao, double DescontoEntidade, double DescontoFinanceiro, int RegimeIva, int Arredondamento, int ArredondamentoIva, double Desconto1, double Desconto2, double Desconto3, double TaxaIva, double PrecUnit, double Quantidade, string CodIva)
		{
			double[] result = null;
			int Apontador = 0;
			TestPrimaveraDarwinSupport.PInvoke.UnsafeNative.Structures.StructCodigosIva EstruturaCodIva = TestPrimaveraDarwinSupport.PInvoke.UnsafeNative.Structures.StructCodigosIva.CreateInstance(); //- Estrutura com os valores/codigos de IVA
			double DescontoTotal = 0;
			bool Servico = false;

			try
			{

				double[] ArrValores = new double[C_DIMARRAY_PRECOS + 1];
				for (int intI = 0; intI <= C_DIMARRAY_PRECOS; intI++)
				{
					ArrValores[intI] = 0;
				}

				Apontador = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_AlocaTotais();
				Servico = !((String.CompareOrdinal(TipoLinha, "10") >= 0 && String.CompareOrdinal(TipoLinha, "20") < 0) || TipoLinha == "40");
				TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_InicializaTotais(Apontador, DescontoEntidade, DescontoFinanceiro,(short) RegimeIva, (short) m_objErpBSO.Base.Params.TipoDesconto,(short) Arredondamento, (short) ArredondamentoIva, PercentagemRetencao, 0);
				DescontoTotal = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaDescontoTotal(Desconto1, Desconto2, Desconto3);

				//BID:535830 -> Estava:  CDbl(Abs(Quantidade))
				//CS.242_7.50_Alfa8 - Adicionado o valor "0" para o parametro "ValorIEC"
				//BID 557608 (foi adicionado o "Arredonda(...,2)" à taxa de Iva)
				LogPRIAPIs.V10_InsereLinha(Apontador, Convert.ToInt32(Conversion.Val(TipoLinha)), TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(TaxaIva, 2), Math.Abs(PrecUnit), DescontoTotal, Quantidade, CodIva, 0, (SujeitoRetencao) ? -1 : 0, 0, 100, 1, 100, 100, 0, "0", 0, 0, 0, 0, 0, true);

				TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_CalculaTotais(Apontador); //- Calcula os Totais para este documento
				TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_ValoresTotaisDocumento(Apontador, ref ArrValores[0], ref EstruturaCodIva); // Preenche os Arrays com os Totais Necessários a partir da UFLCALC.DLL

				if (Servico)
				{
					ArrValores[9] = ArrValores[1];
					ArrValores[1] = 0;
					ArrValores[10] = ArrValores[2];
					ArrValores[2] = 0;
				}

				result = ArrValores;

				TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lcalc100.V10_LibertaTotais(Apontador);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_GpBSVendas.CalculaPrecoLinha", excep.Message);
			}

			return result;
		}

		public void ActualizaValorAtributo(string Filial, string TipoDoc, string Serie, int Numdoc, string Atributo, dynamic Valor)
		{
			StdBE100.StdBECampos objCampos = null;

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (Erro)");

Inicio:

			m_objErpBSO.IniciaTransaccao();

			objCampos = DaValorAtributos(Filial, TipoDoc, Serie, Numdoc, "Assinatura"); //PriGlobal: IGNORE

			string tempRefParam = "Assinatura";
			if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(DaValorAtributo(Filial, TipoDoc, Serie, Numdoc, tempRefParam))) > 0)
			{

				//Valida se o atributo é passível de ser alterado
				CertificacaoSoftware.ValidaAtributoAlteravel(Atributo);

			}

			//Pode alterar o atributo
			m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributo(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref Atributo, ref Valor);

			m_objErpBSO.TerminaTransaccao();

			return;

Erro:
			m_objErpBSO.DesfazTransaccao();
			if (m_objErpBSO.VerificaErroLock())
			{
				//UPGRADE_TODO: (1065) Error handling statement (Inicio) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
				UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("Resume Label (Inicio)");
			}
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaValorAtributo", Information.Err().Description);

		}

		public void ActualizaValorAtributos(string Filial, string TipoDoc, string Serie, int Numdoc, StdBECampos Atributos)
		{
			string Atributo = "";

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (Erro)");

			if ((Atributos == null))
			{
				StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.ActualizaValorAtributos", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
			}
			if ((Atributos.NumItens == 0))
			{
				return;
			}

Inicio:

			m_objErpBSO.IniciaTransaccao();

			string tempRefParam = "Assinatura";
			if (Strings.Len(ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Vendas.Documentos.DaValorAtributo(Filial, TipoDoc, Serie, Numdoc, tempRefParam))) > 0)
			{

				int tempForVar = Atributos.NumItens;
				for (int lngAtr = 1; lngAtr <= tempForVar; lngAtr++)
				{

					string tempRefParam2 = lngAtr.ToString();
					Atributo = Atributos.GetItem(ref tempRefParam2).Nome;
					lngAtr = Convert.ToInt32(Double.Parse(tempRefParam2));

					//Valida se o atributo é passível de ser alterado
					CertificacaoSoftware.ValidaAtributoAlteravel(Atributo);

				}

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributos(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref Atributos);

			}
			else
			{

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributos(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref Atributos);

			}

			m_objErpBSO.TerminaTransaccao();

			return;

Erro:
			m_objErpBSO.DesfazTransaccao();
			if (m_objErpBSO.VerificaErroLock())
			{
				//UPGRADE_TODO: (1065) Error handling statement (Inicio) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
				UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("Resume Label (Inicio)");
			}
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaValorAtributos", Information.Err().Description);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributo
		// Description :
		// Arguments   : Filial   -->
		// Arguments   : TipoDoc  -->
		// Arguments   : Serie    -->
		// Arguments   : NumDoc   -->
		// Arguments   : Atributo -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public dynamic DaValorAtributo(string Filial, string TipoDoc, string Serie, int Numdoc, string Atributo)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributo(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref Atributo);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributo", excep.Message);
			}
			return null;
		}

        //---------------------------------------------------------------------------------------
        // Procedure   : IVndBSVendas_DaValorAtributosId
        // Description :
        // Arguments   : Id -->
        // Returns     : None
        //---------------------------------------------------------------------------------------
        public StdBE100.StdBECampos DaValorAtributosID(string ID, params dynamic[] Atributos)
		{
			StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributosID", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempRefParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosID(ID, ref tempRefParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributosID", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributosIDLinha
		// Description :
		// Arguments   : sID -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBECampos DaValorAtributosIDLinha(string sID, params dynamic[] Atributos)
		{
			StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributosIDLinha", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempRefParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributosIDLinha(ref sID, ref tempRefParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributosIDLinha", excep.Message);
			}

			return result;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_DaValorAtributos
		// Description :
		// Arguments   : Filial  -->
		// Arguments   : TipoDoc -->
		// Arguments   : Serie   -->
		// Arguments   : NumDoc  -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBECampos DaValorAtributos(string Filial, string TipoDoc, string Serie, int Numdoc, params dynamic[] Atributos)
		{
			StdBECampos result = null;
			string[] SAtributos = null;

			try
			{

				if ((false))
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.DaValorAtributos", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}

				for (int i = 0; i <= Atributos.GetUpperBound(0); i++)
				{

					SAtributos = ArraysHelper.RedimPreserve(SAtributos, new int[]{i + 1});
					SAtributos[i] = ReflectionHelper.GetPrimitiveValue<string>(Atributos[i]);

				}

				Array tempRefParam = SAtributos;
				result = m_objErpBSO.DSO.Vendas.Documentos.DaValorAtributos(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref tempRefParam);
				SAtributos = ArraysHelper.CastArray<string[]>(tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaValorAtributos", excep.Message);
			}

			return result;
		}

		//Devolve a quantidade reservada de uma linha de venda.
		public double DaQuantReservadaID(string IdLinha)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaQntReservada(ref IdLinha);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaQuantReservadaID", excep.Message);
			}
			return 0;
		}

		public double DaQuantReservada(string Filial, string TipoDoc, string strSerie, int Numdoc, int NumLinha)
		{

			try
			{

				string tempRefParam = DaIDLinha(Filial, TipoDoc, strSerie, Numdoc, NumLinha);

				return m_objErpBSO.DSO.Vendas.Documentos.DaQntReservada(ref tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaQuantReservada", excep.Message);
			}
			return 0;
		}

		public string DaIDLinha(string Filial, string TipoDoc, string Serie, int Numdoc, int NumLinha)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaIdLinha(ref Filial, ref TipoDoc, ref Serie, ref Numdoc, ref NumLinha);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaIdLinha", excep.Message);
			}
			return "";
		}

		public string DaId(string Filial, string TipoDoc, string Serie, int Numdoc)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaId(ref Filial, ref TipoDoc, ref Serie, ref Numdoc);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaId", excep.Message);
			}
			return "";
		}
		public double DaQuantTransformada(string Filial, string TipoDoc, string strSerie, int Numdoc, int NumLinha)
		{
			try
			{


				string tempRefParam = DaIDLinha(Filial, TipoDoc, strSerie, Numdoc, NumLinha);

				return m_objErpBSO.DSO.Vendas.Documentos.DaQntTransformada(ref tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaQuantTransformada", excep.Message);
			}
			return 0;
		}

		public double DaQuantTransformadaID(string IdLinha)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaQntTransformada(ref IdLinha);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaQuantTransformada", excep.Message);
			}
			return 0;
		}

		public void ActualizaBDTemp( VndBEDocumentoVenda clsDocumentoVenda,  string Strtabelacab,  string Strtabelalinha,  string Strtabelanumserie,  string StrTabelaDim,  string strTabelaReservas,  string strAvisos)
		{
			VndBE100.VndBETabVenda clsTabVenda = null;
			int i = 0;
			int Mult = 0;
			bool blnModoEdicao = false;
			bool IniciouTrans = false;
			StdBE100.StdBELista objLst = null;

			try
			{



				//Calcular valores totais do documento de venda
				CalculaValoresTotais(ref clsDocumentoVenda);

				m_objErpBSO.IniciaTransaccao();
				IniciouTrans = true;

				//Bloqueia registos
				BloqueiaRegistosDocumentoVenda(clsDocumentoVenda);

				clsDocumentoVenda.DataDoc = DateTime.Parse(clsDocumentoVenda.DataDoc.ToString("d"));
				clsDocumentoVenda.DataVenc = DateTime.Parse(clsDocumentoVenda.DataVenc.ToString("d"));
				clsDocumentoVenda.Utilizador = m_objErpBSO.Contexto.UtilizadorActual;
				//Edita a configuração do documento de venda
				string tempParam = clsDocumentoVenda.Tipodoc;
				clsTabVenda = m_objErpBSO.Vendas.TabVendas.Edita(tempParam);
				if (!clsDocumentoVenda.EmModoEdicao)
				{
					if (clsTabVenda.TipoDocumento == ((int) BasBETipos.LOGTipoDocumento.LOGDocCotacao))
					{
						clsDocumentoVenda.Estado = "P";
					}
				}

				//Se o documento é a pagar os valores são gravados com sinal negativo
				if (clsTabVenda.PagarReceber == "P")
				{
					Mult = -1;
				}
				else
				{
					Mult = 1;
				}

				//** Preenche o objecto documento de venda com os dados por defeito
				PreencheDocVendaBDTemp(clsDocumentoVenda);

				blnModoEdicao = clsDocumentoVenda.EmModoEdicao;

				if (!clsDocumentoVenda.EmModoEdicao)
				{

					//FIL
					if (clsDocumentoVenda.Filial == m_objErpBSO.Base.Filiais.CodigoFilial)
					{

						string tempParam2 = "SELECT Max(NumDoc) as UltimoNumero FROM " + Strtabelacab;
						objLst = m_objErpBSO.Consulta(tempParam2);
						//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
						if (Convert.IsDBNull(objLst.Valor("UltimoNumero")))
						{
							clsDocumentoVenda.NumDoc = m_objErpBSO.Base.Series.ProximoNumero("V", clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, true);
						}
						else if (Strings.Len(objLst.Valor("UltimoNumero")) == 0)
						{ 
							clsDocumentoVenda.NumDoc = m_objErpBSO.Base.Series.ProximoNumero("V", clsDocumentoVenda.Tipodoc, clsDocumentoVenda.Serie, true);
						}
						else
						{
							clsDocumentoVenda.NumDoc = Convert.ToInt32(Double.Parse(objLst.Valor("UltimoNumero")) + 1);
						}

						objLst = null;
					}
				}
				else
				{
					string tempParam3 = clsDocumentoVenda.Filial;
					string tempParam4 = clsDocumentoVenda.Tipodoc;
					string tempParam5 = clsDocumentoVenda.Serie;
					int tempParam6 = clsDocumentoVenda.NumDoc;
					m_objErpBSO.DSO.Vendas.Documentos.RemoveBDTemp( Strtabelacab,  Strtabelalinha,  Strtabelanumserie,  strTabelaReservas,  tempParam3,  tempParam4,  tempParam5,  tempParam6,  StrTabelaDim);
					clsDocumentoVenda.EmModoEdicao = false;
				}

				clsDocumentoVenda.EmModoEdicao = blnModoEdicao;

				//Aplica o multiplicador nas linhas
				AplicaMultiplicador(Mult, clsDocumentoVenda);

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaCabecBDTemp( Strtabelacab,  clsDocumentoVenda);

				i = 1;
				foreach (VndBE100.VndBELinhaDocumentoVenda LinhaVenda in clsDocumentoVenda.Linhas)
				{

					//Se o artigo não tem tratamento de lotes grava o artigo com o lote por defeito
					//Se o artigo estiver preenchido
					if (Strings.Len(LinhaVenda.Artigo) != 0)
					{

						if (~ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Artigos.DaValorAtributo(LinhaVenda.Artigo, "TratamentoLotes")) != 0)
						{

							LinhaVenda.Lote = ConstantesPrimavera100.Inventario.LotePorDefeito;

						}

					}

					//** Actualiza os números de série
					foreach (BasBENumeroSerie LinhaNumSerie in LinhaVenda.NumerosSerie)
					{

						//Insere LinhasNumSerie e remove na tabela ArtigoNumSerie
						string tempParam7 = LinhaVenda.IdLinha;
						m_objErpBSO.DSO.Vendas.Documentos.ActualizaSaidaNumSerieBDTemp( Strtabelanumserie,  tempParam7, LinhaNumSerie);

					}

					m_objErpBSO.DSO.Vendas.Documentos.ActualizaLinhasBDTemp( Strtabelalinha,  clsDocumentoVenda,  i);

					//** Actualiza as dimensões das linhas
					//            For Each clsLinhaDim In LinhaVenda.LinhasDimensoes
					//                m_objErpBSO.DSO.Vendas.Documentos.ActualizaLinhasDimTemp StrTabelaDim, LinhaVenda.IdLinha, clsLinhaDim.IdArtigoDimensao, clsLinhaDim.Quantidade
					//            Next

					//** Actualiza as reservas das linhas
					foreach (dynamic clsLinhaReserva in LinhaVenda.ReservaStock.Linhas)
					{
						m_objErpBSO.DSO.Vendas.Documentos.ActualizaLinhaReservaBDTemp( strTabelaReservas, clsLinhaReserva);
					}

					i++;

				}


				m_objErpBSO.TerminaTransaccao();
				IniciouTrans = false;

				clsTabVenda = null;
			}
			catch (System.Exception excep)
			{

				if (IniciouTrans)
				{
					m_objErpBSO.DesfazTransaccao();
				}

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				throw new System.Exception(Information.Err().Number.ToString() + ", _VNDBSVendas.ActualizaBDTemp, " + excep.Message);
			}

		}

		public void ActualizaBDTemp( VndBEDocumentoVenda clsDocumentoVenda,  string Strtabelacab,  string Strtabelalinha,  string Strtabelanumserie,  string StrTabelaDim,  string strTabelaReservas)
		{
			string tempParam445 = "";
			ActualizaBDTemp( clsDocumentoVenda,  Strtabelacab,  Strtabelalinha,  Strtabelanumserie,  StrTabelaDim,  strTabelaReservas,  tempParam445);
		}


		//Preenche o objecto documento de venda antes de fazer a actualização.
		private void PreencheDocVendaBDTemp(VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{

			try
			{


				if (Strings.Len(clsDocumentoVenda.ID) == 0)
				{

					bool tempRefParam = true;
					clsDocumentoVenda.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);

				}

				//** Preenche o objecto com a filial
				//FIL
				if (Strings.Len(clsDocumentoVenda.Filial) == 0)
				{

					clsDocumentoVenda.Filial = m_objErpBSO.Base.Filiais.CodigoFilial;

				}

				//Se a moeda não estiver preenchida é a moeda do cliente
				if (Strings.Len(clsDocumentoVenda.Moeda) == 0)
				{

					if (clsDocumentoVenda.TipoEntidade == "C")
					{

						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Clientes.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						clsDocumentoVenda.Moeda = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Clientes.DaValorAtributo(clsDocumentoVenda.Entidade, "Moeda"));

					}
					else
					{

						//UPGRADE_WARNING: (1068) m_objErpBSO.Base.OutrosTerceiros.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
						clsDocumentoVenda.Moeda = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.OutrosTerceiros.DaValorAtributo(clsDocumentoVenda.Entidade, clsDocumentoVenda.TipoEntidade, "Moeda"));

					}

				}

				if (clsDocumentoVenda.DataUltimaActualizacao == DateTime.FromOADate(0))
				{

					clsDocumentoVenda.DataUltimaActualizacao = DateTime.Now;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				throw new System.Exception(Information.Err().Number.ToString() + ", _VNDBSVendas.PreencheDocVendaBDTemp, " + excep.Message);
			}


		}


		//Remove o documento de venda da base de dados temporária.
		public void RemoveBDTemp( string Strtabelacab,  string Strtabelalinha,  string Strtabelanumserie,  string strTabelaReservas,  string Filial,  string TipoDoc,  string strSerie,  int Numdoc,  string StrTabelaDim)
		{

			try
			{

				m_objErpBSO.IniciaTransaccao();
				m_objErpBSO.DSO.Vendas.Documentos.RemoveBDTemp( Strtabelacab,  Strtabelalinha,  Strtabelanumserie,  strTabelaReservas,  Filial,  TipoDoc,  strSerie,  Numdoc,  StrTabelaDim);
				m_objErpBSO.TerminaTransaccao();
			}
			catch (System.Exception excep)
			{

				m_objErpBSO.DesfazTransaccao();
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				throw new System.Exception(Information.Err().Number.ToString() + ", _VNDBSVendas.PreencheDocVendaBDTemp, " + excep.Message);
			}

		}

		public bool EDevolucao(string DocOrigem, string DocDestino)
		{

			//informação do documento origem
			dynamic[] tempRefParam = new dynamic[]{"TipoDocumento", "PagarReceber"};
			StdBE100.StdBECampos Campos = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(DocOrigem, tempRefParam);
			//UPGRADE_WARNING: (1068) Campos().Valor of type Variant is being forced to LOGTipoDocumento. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
			string tempRefParam2 = "TipoDocumento";
			BasBETipos.LOGTipoDocumento TipoDocumentoOrigem = ReflectionHelper.GetPrimitiveValue<BasBETipos.LOGTipoDocumento>(Campos.GetItem(ref tempRefParam2).Valor);
			//UPGRADE_WARNING: (1068) Campos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
			string tempRefParam3 = "PagarReceber";
			string NaturezaDocOrigem = ReflectionHelper.GetPrimitiveValue<string>(Campos.GetItem(ref tempRefParam3).Valor);
			Campos = null;

			//informação do documento destino
			dynamic[] tempRefParam4 = new dynamic[]{"TipoDocumento", "PagarReceber"};
			Campos = m_objErpBSO.Vendas.TabVendas.DaValorAtributos(DocDestino, tempRefParam4);
			//UPGRADE_WARNING: (1068) Campos().Valor of type Variant is being forced to LOGTipoDocumento. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
			string tempRefParam5 = "TipoDocumento";
			BasBETipos.LOGTipoDocumento TipoDocumentoDestino = ReflectionHelper.GetPrimitiveValue<BasBETipos.LOGTipoDocumento>(Campos.GetItem(ref tempRefParam5).Valor);
			//UPGRADE_WARNING: (1068) Campos().Valor of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
			string tempRefParam6 = "PagarReceber";
			string NaturezaDocDestino = ReflectionHelper.GetPrimitiveValue<string>(Campos.GetItem(ref tempRefParam6).Valor);
			Campos = null;

			return (TipoDocumentoOrigem == TipoDocumentoDestino) && (TipoDocumentoDestino == BasBETipos.LOGTipoDocumento.LOGDocFinanceiro) && (NaturezaDocDestino != NaturezaDocOrigem);

		}

		public bool ValidaDevolucao(System.DateTime DataOrigem, System.DateTime DataDestino, string Artigo)
		{

			return (ReflectionHelper.GetPrimitiveValue<int>(m_objErpBSO.Base.Artigos.DaValorAtributo(Artigo, "PermiteDevolucao")) & ((DataOrigem.ToOADate() - DataDestino.ToOADate() <= m_objErpBSO.Vendas.Params.NumeroDiasDevolucao) ? -1 : 0)) != 0;

		}

		// Fecha uma linha pelo seu Identificador
		public void FechaItem(string Id)
		{
			StdBE100.StdBELista objCampos = null;
			double dblTotalLinha = 0;

			try
			{

				//Fecha a Linha
				string tempRefParam = (Id);
				m_objErpBSO.DSO.Vendas.Documentos.FechaItem(ref tempRefParam);

				//Devolve os dados necessários para o calculo do valor da linha
				dblTotalLinha = DaValoresLinhaTrans(ref Id, ref objCampos);

				//Actualiza o valor da linha
				if (objCampos.Valor("TipoDocumento") == ((int) BasBETipos.LOGTipoDocumento.LOGDocFinanceiro).ToString())
				{
					if (objCampos.Valor("TipoEntidade") == ConstantesPrimavera100.TiposEntidade.Cliente)
					{
						m_objErpBSO.Base.Clientes.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), false, false);
					}
					else
					{
						m_objErpBSO.Base.OutrosTerceiros.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), false, false);
					}
				}
				else if (objCampos.Valor("TipoDocumento") == ((int) BasBETipos.LOGTipoDocumento.LOGDocStk_Transporte).ToString())
				{ 
					if (objCampos.Valor("TipoEntidade") == ConstantesPrimavera100.TiposEntidade.Cliente)
					{
						m_objErpBSO.Base.Clientes.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), true, false);
					}
					else
					{
						m_objErpBSO.Base.OutrosTerceiros.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), true, false);
					}
				}
				else if (objCampos.Valor("TipoDocumento") == ((int) BasBETipos.LOGTipoDocumento.LOGDocEncomenda).ToString())
				{ 

					if (objCampos.Valor("TipoEntidade") == ConstantesPrimavera100.TiposEntidade.Cliente)
					{
						m_objErpBSO.Base.Clientes.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), false, true);
					}
					else
					{
						m_objErpBSO.Base.OutrosTerceiros.ActualizaTotalDebito(-dblTotalLinha, objCampos.Valor("Entidade"), Double.Parse(objCampos.Valor("Cambio")), Double.Parse(objCampos.Valor("CambioMBase")), Double.Parse(objCampos.Valor("CambioMAlt")), false, true);
					}
				}

				//Calcula o novo estado do documento
				string tempRefParam2 = objCampos.Valor("IdCabecDoc");
				short tempRefParam3 = (short) m_objErpBSO.Inventario.Params.CasasDecimaisQnt;
				m_objErpBSO.DSO.Vendas.Documentos.ActualizaEstTransDocOrigem(ref tempRefParam2, ref tempRefParam3);
				m_objErpBSO.Inventario.Params.CasasDecimaisQnt = tempRefParam3;

				TrataReservasFechoLinha("'" + Id + "'");
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.FechaItem", excep.Message);
			}

		}

		public StdBELista LstLinhasDocVendasNumerosSerieNaoTrans(string QueryTipo, string LinhaDocId, string Artigo, string Cliente)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.LstLinhasDocVendasNumerosSerieNaoTrans(ref QueryTipo, ref LinhaDocId, ref Artigo, ref Cliente);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstLinhasDocVendasNumerosSerieNaoTrans", excep.Message);
			}
			return null;
		}

		public StdBELista LstLinhasDocVendasNumerosSerieNaoTrans(string QueryTipo, string LinhaDocId, string Artigo)
		{
			string tempRefParam446 = "";
			return LstLinhasDocVendasNumerosSerieNaoTrans( QueryTipo,  LinhaDocId,  Artigo,  tempRefParam446);
		}

		public StdBELista LstLinhasDocVendasNumerosSerieNaoTrans(string QueryTipo,  string LinhaDocId)
		{
			string tempRefParam447 = "";
			string tempRefParam448 = "";
			return LstLinhasDocVendasNumerosSerieNaoTrans( QueryTipo,  LinhaDocId,  tempRefParam447,  tempRefParam448);
		}

		public StdBELista LstLinhasDocVendasNumerosSerieNaoTrans( string QueryTipo)
		{
			string tempRefParam449 = "";
			string tempRefParam450 = "";
			string tempRefParam451 = "";
			return LstLinhasDocVendasNumerosSerieNaoTrans( QueryTipo,  tempRefParam449,  tempRefParam450,  tempRefParam451);
		}

		public StdBELista LstLinhasDocVendasNumerosSerieNaoTrans()
		{
			string tempRefParam452 = "";
			string tempRefParam453 = "";
			string tempRefParam454 = "";
			string tempRefParam455 = "";
			return LstLinhasDocVendasNumerosSerieNaoTrans( tempRefParam452,  tempRefParam453,  tempRefParam454,  tempRefParam455);
		}

		public StdBELista LstComissoesVendedores(string strFilial, string strVendedorInicial, string strVendedorFinal, System.DateTime datDataInicial, System.DateTime datDataFinal, string strDocumentos, string strSeries, bool blnVendas, bool blnLiquidacoes, byte bytTipoCalculoLiquidacoes, bool blnChefes, StdBELista objComissoesChefes, string strNomeTabelaComissoes, string strNomeTabelaComissoesChefe, bool blnMantemTabelas)
		{

			try
			{
				return m_objErpBSO.DSO.Vendas.Documentos.LstComissoesVendedores(ref strFilial, ref strVendedorInicial, ref strVendedorFinal, ref datDataInicial, ref datDataFinal, ref strDocumentos, ref strSeries, ref blnVendas, ref blnLiquidacoes, ref bytTipoCalculoLiquidacoes, ref blnChefes, ref objComissoesChefes, ref strNomeTabelaComissoes, ref strNomeTabelaComissoesChefe, ref blnMantemTabelas);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstComissoesVendedores", excep.Message);
			}
			return null;
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes,  byte bytTipoCalculoLiquidacoes,  bool blnChefes,  StdBELista objComissoesChefes,  string strNomeTabelaComissoes,  string strNomeTabelaComissoesChefe)
		{
			bool tempRefParam456 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  bytTipoCalculoLiquidacoes,  blnChefes,  objComissoesChefes,  strNomeTabelaComissoes,  strNomeTabelaComissoesChefe,  tempRefParam456);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes,  byte bytTipoCalculoLiquidacoes,  bool blnChefes,  StdBELista objComissoesChefes,  string strNomeTabelaComissoes)
		{
			string tempRefParam457 = "";
			bool tempRefParam458 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  bytTipoCalculoLiquidacoes,  blnChefes,  objComissoesChefes,  strNomeTabelaComissoes,  tempRefParam457,  tempRefParam458);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes,  byte bytTipoCalculoLiquidacoes,  bool blnChefes,  StdBELista objComissoesChefes)
		{
			string tempRefParam459 = "";
			string tempRefParam460 = "";
			bool tempRefParam461 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  bytTipoCalculoLiquidacoes,  blnChefes,  objComissoesChefes,  tempRefParam459,  tempRefParam460,  tempRefParam461);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes,  byte bytTipoCalculoLiquidacoes,  bool blnChefes)
		{
			StdBELista tempRefParam462 = null;
			string tempRefParam463 = "";
			string tempRefParam464 = "";
			bool tempRefParam465 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  bytTipoCalculoLiquidacoes,  blnChefes,  tempRefParam462,  tempRefParam463,  tempRefParam464,  tempRefParam465);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes,  byte bytTipoCalculoLiquidacoes)
		{
			bool tempRefParam466 = false;
			StdBELista tempRefParam467 = null;
			string tempRefParam468 = "";
			string tempRefParam469 = "";
			bool tempRefParam470 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  bytTipoCalculoLiquidacoes,  tempRefParam466,  tempRefParam467,  tempRefParam468,  tempRefParam469,  tempRefParam470);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas,  bool blnLiquidacoes)
		{
			byte tempRefParam471 = 0;
			bool tempRefParam472 = false;
			StdBELista tempRefParam473 = null;
			string tempRefParam474 = "";
			string tempRefParam475 = "";
			bool tempRefParam476 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  blnLiquidacoes,  tempRefParam471,  tempRefParam472,  tempRefParam473,  tempRefParam474,  tempRefParam475,  tempRefParam476);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries,  bool blnVendas)
		{
			bool tempRefParam477 = true;
			byte tempRefParam478 = 0;
			bool tempRefParam479 = false;
			StdBELista tempRefParam480 = null;
			string tempRefParam481 = "";
			string tempRefParam482 = "";
			bool tempRefParam483 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  blnVendas,  tempRefParam477,  tempRefParam478,  tempRefParam479,  tempRefParam480,  tempRefParam481,  tempRefParam482,  tempRefParam483);
		}

		public StdBELista LstComissoesVendedores( string strFilial,  string strVendedorInicial,  string strVendedorFinal,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strDocumentos,  string strSeries)
		{
			bool tempRefParam484 = true;
			bool tempRefParam485 = true;
			byte tempRefParam486 = 0;
			bool tempRefParam487 = false;
			StdBELista tempRefParam488 = null;
			string tempRefParam489 = "";
			string tempRefParam490 = "";
			bool tempRefParam491 = false;
			return LstComissoesVendedores( strFilial,  strVendedorInicial,  strVendedorFinal,  datDataInicial,  datDataFinal,  strDocumentos,  strSeries,  tempRefParam484,  tempRefParam485,  tempRefParam486,  tempRefParam487,  tempRefParam488,  tempRefParam489,  tempRefParam490,  tempRefParam491);
		}

		public StdBELista LstResumoVendas( string strMoedaVisualizacao,  string strFilial,  string strDocumentos,  string strSeries,  string strOrigem,  string strTipo,  System.DateTime datDataInicial,  System.DateTime datDataFinal,  string strTipoEntidade,  string strEntidade,  string strVendedor,  string strZona,  string strSeccao,  string strArtigo,  string strFamilia,  string strObraID,  string strUtilizador,  string strPosto)
		{

			try
			{
				bool tempRefParam = strMoedaVisualizacao == m_objErpBSO.Contexto.MoedaBase;
				byte tempRefParam2 = (byte) m_objErpBSO.Contexto.SentidoCambios;
				bool tempRefParam3 = m_objErpBSO.Contexto.CambioTrabalho == ErpBS100.StdBEContexto.EnumCambioTrabalho.ctActual;
				return m_objErpBSO.DSO.Vendas.Documentos.LstResumoVendas( strMoedaVisualizacao,  tempRefParam,  tempRefParam2,  tempRefParam3,  strFilial,  strDocumentos,  strSeries,  strOrigem,  strTipo,  datDataInicial,  datDataFinal,  strTipoEntidade,  strEntidade,  strVendedor,  strZona,  strSeccao,  strArtigo,  strFamilia,  strObraID,  strUtilizador,  strPosto);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstResumoVendas", excep.Message);
			}
			return null;
		}

		public StdBELista LstResumoEncomendas( string strMoedaVisualizacao,  bool blnUnidadeBase,  string strFilial,  string strDocumentos,  string strOrigem,  string strTipoEntidade,  string strEntidadeInicial,  string strEntidadeFinal,  string strArtigoInicial,  string strArtigoFinal,  string strObraID)
		{

			try
			{
				return m_objErpBSO.DSO.Vendas.Documentos.LstResumoEncomendas( strMoedaVisualizacao,  blnUnidadeBase,  strFilial,  strDocumentos,  strOrigem,  strTipoEntidade,  strEntidadeInicial,  strEntidadeFinal,  strArtigoInicial,  strArtigoFinal,  strObraID);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstResumoEncomendas", excep.Message);
			}
			return null;
		}


		public StdBELista LstCabecalhoDocVenda( string strFilial,  string strSeries,  string strTipoDocumento,  string strEntidade)
		{
			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.LstCabecalhoDocVenda( strFilial,  strSeries,  strTipoDocumento,  strEntidade);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstCabecalhoDocVenda", excep.Message);
			}
			return null;
		}

		//FIL
		private void RegistaLinhasRemovidas(VndBE100.VndBELinhasDocumentoVenda LinhasVenda, VndBE100.VndBELinhasDocumentoVenda LinhasVendaBD)
		{


			foreach (VndBE100.VndBELinhaDocumentoVenda clsLinhaVendaBD in LinhasVendaBD)
			{

				if (!ExisteLinha(LinhasVenda, clsLinhaVendaBD.IdLinha))
				{
					m_objErpBSO.Base.Filiais.RegistaRemocao("LinhasDoc", clsLinhaVendaBD.IdLinha); //PriGlobal: IGNORE
				}

			}

		}

		private bool ExisteLinha(VndBE100.VndBELinhasDocumentoVenda LinhasVenda, string Id)
		{

			bool result = false;
			foreach (VndBE100.VndBELinhaDocumentoVenda clsLinhaVendaBD in LinhasVenda)
			{

				if (clsLinhaVendaBD.IdLinha == Id)
				{
					return true;
				}

			}

			return result;
		}

		public bool ExisteDimEmColeccao(VndBELinhasDocumentoVenda clsLinhasVenda, string strArtigo, string IdLinhaPai)
		{

			bool result = false;
			VndBE100.VndBELinhaDocumentoVenda clsLStk = null;
			try
			{

				result = false;
				foreach (VndBE100.VndBELinhaDocumentoVenda clsLStk2 in clsLinhasVenda)
				{
					clsLStk = clsLStk2;
					if (clsLStk.Artigo == strArtigo)
					{
						result = true;
						break;
					}
					clsLStk = null;
				}

				clsLStk = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ExisteDimEmColeccao", excep.Message);
			}
			return result;
		}

		public bool ExisteDimEmColeccao( VndBELinhasDocumentoVenda clsLinhasVenda,  string strArtigo)
		{
			string tempRefParam492 = "";
			return ExisteDimEmColeccao(clsLinhasVenda, strArtigo, tempRefParam492);
		}

		//GCPBS
		public StdBELista LstCreditosUtilizador( string TipoEntidade, string Entidade, string Moeda, string TipoConta, string sNatCV, string strCodigoFilial)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.LstCreditosUtilizador(ref TipoEntidade, ref Entidade, ref Moeda, ref TipoConta, ref sNatCV, ref strCodigoFilial);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.LstCreditosUtilizador", excep.Message);
			}
			return null;
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : PreencheXMLHistorico
		// Description :
		// Arguments   : objDocVenda -->
		// Arguments   : clsTabVenda -->
		// Arguments   : NumPrest    -->
		// Arguments   : objXML      -->
		// Arguments   : strID       -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void PreencheXMLHistorico(VndBE100.VndBEDocumentoVenda objDocVenda, VndBE100.VndBETabVenda clsTabVenda, int NumPrest, StdBE100.StdBESpXml objXML, string strID)
		{
			StdBE100.StdBELista stdEstorno = null;
			VndBE100.VndBELinhaDocumentoVenda ObjLinha = null; //BID:561557
			StdBE100.StdBELista Campo = null;
			StdBE100.StdBELista objLista = null;
			double dblTotalIva = 0;
			double dblTotalRecargo = 0;
			double dblTotalAdiantamentos = 0;
			dynamic varValor = null;
			string strIDLinha = "";
			int lngNumeroLinha = 0;
			bool blnLinhasEstorno = false;
			StdBE100.StdBECampos objCampos = null;
			double dblAdiantamentoLinha = 0;
			double dblDifCambio = 0;
			double dblCorrecaoMonetaria = 0;
			//CS.3405 - Adiantamentos
			bool blnNovosAdiantamentos = false;
			double dblTotalAdiDoc = 0;
			double dblTotalIvaAdiDoc = 0;
			double dblUltValorPrestacao = 0; //BID 589198
			double dblUltValorIva = 0; //BID 589198
			double dblUltValorRecargo = 0; //BID 589198
			bool blnIvaIvaIndiferido = false;

			try
			{

				//CS.3405 - Adiantamentos
				blnNovosAdiantamentos = false;

				objXML.AdicionaNodo("<HST"); //PriGlobal: IGNORE

				//CS.3405 - Adiantamentos
				dblTotalAdiDoc = 0;

				//BID 589198 : foi adicionado o teste 'Or (objDocVenda.Prestacoes.NumItens > 0 And Not objDocVenda.GeraPendentePorLinha)'
				if (objDocVenda.Prestacoes.NumItens == 0 || (objDocVenda.Prestacoes.NumItens > 0 && !objDocVenda.GeraPendentePorLinha))
				{

					foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha2 in objDocVenda.Linhas)
					{
						ObjLinha = ObjLinha2;

						if (ObjLinha.TipoLinha == ConstantesPrimavera100.Documentos.TipoLinAdiantamentos && Strings.Len(ObjLinha.DadosAdiantamento.IDHistorico) > 0)
						{

							blnNovosAdiantamentos = true;
							dblTotalAdiDoc = dblTotalAdiDoc + ObjLinha.PrecoLiquido + ObjLinha.TotalIva;
							dblTotalIvaAdiDoc = ObjLinha.TotalIva;

						}

						ObjLinha = null;
					}


				}
				//END CS.3405


				objXML.AdicionaCampo("ID", strID); //PriGlobal: IGNORE
				objXML.AdicionaCampo("IDD", objDocVenda.ID); //PriGlobal: IGNORE
				objXML.AdicionaCampo("FIL", objDocVenda.Filial); //PriGlobal: IGNORE
				objXML.AdicionaCampo("M", ConstantesPrimavera100.Modulos.Vendas); //PriGlobal: IGNORE
				objXML.AdicionaCampo("TD", objDocVenda.Tipodoc); //PriGlobal: IGNORE
				objXML.AdicionaCampo("ND", objDocVenda.NumDoc.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampo("NDI", objDocVenda.NumDoc.ToString()); //PriGlobal: IGNORE

				objXML.AdicionaCampo("NP", NumPrest.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampo("SE", objDocVenda.Serie); //PriGlobal: IGNORE
				objXML.AdicionaCampo("TE", objDocVenda.TipoEntidade); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("CMB", objDocVenda.Cambio.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("CMBMB", objDocVenda.CambioMBase.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("CMBMA", objDocVenda.CambioMAlt.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampo("CPG", objDocVenda.CondPag); //PriGlobal: IGNORE

				objXML.AdicionaCampoData("DTD", objDocVenda.DataDoc); //PriGlobal: IGNORE
				objXML.AdicionaCampoData("DTE", objDocVenda.DataDoc); //PriGlobal: IGNORE
				objXML.AdicionaCampoData("DTI", objDocVenda.DataDoc); //PriGlobal: IGNORE

				objXML.AdicionaCampo("EN", objDocVenda.Entidade); //PriGlobal: IGNORE
				objXML.AdicionaCampo("MPG", objDocVenda.ModoPag); //PriGlobal: IGNORE
				objXML.AdicionaCampo("CD", objDocVenda.ContaDomiciliacao); //PriGlobal: IGNORE
				objXML.AdicionaCampo("MO", objDocVenda.Moeda); //PriGlobal: IGNORE
				objXML.AdicionaCampoLogico("MUEM", objDocVenda.MoedaDaUEM); //PriGlobal: IGNORE
				objXML.AdicionaCampo("TC", clsTabVenda.TipoConta); //PriGlobal: IGNORE

				objXML.AdicionaCampo("TL", objDocVenda.TipoLancamento); //PriGlobal: IGNORE

				objXML.AdicionaCampo("IDCBL", objDocVenda.IDCabecMovCbl); //PriGlobal: IGNORE

				objXML.AdicionaCampo("ENF", objDocVenda.EntidadeFac); //PriGlobal: IGNORE
				objXML.AdicionaCampo("TEF", objDocVenda.TipoEntidadeFac); //PriGlobal: IGNORE

				//CS.2243
				objXML.AdicionaCampoLogico("CADD", objDocVenda.CambioADataDoc); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("DIFCMA", "0"); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("DIFAR", "0"); //PriGlobal: IGNORE

				objXML.AdicionaCampo("LOP", objDocVenda.LocalOperacao); //BID 577005 'PriGlobal: IGNORE

				objXML.AdicionaCampoNumerico("TTC", objDocVenda.TipoTerceiro);

				if (objDocVenda.Linhas.NumItens > 0)
				{

					//BID 556890
					if (objDocVenda.GeraPendentePorLinha)
					{
						if (NumPrest > 0 && NumPrest <= objDocVenda.Linhas.NumItens)
						{
							if (Strings.Len(objDocVenda.Linhas.GetEdita(NumPrest).Vendedor) > 0)
							{
								objXML.AdicionaCampo("VD", objDocVenda.Linhas.GetEdita(NumPrest).Vendedor.ToUpper()); //PriGlobal: IGNORE
							}
						}
					}
					else
					{
						//Fim 556890
						int tempForVar = objDocVenda.Linhas.NumItens;
						for (int lngLinha = 1; lngLinha <= tempForVar; lngLinha++)
						{

							if (Strings.Len(objDocVenda.Linhas.GetEdita(lngLinha).Vendedor) > 0)
							{

								objXML.AdicionaCampo("VD", objDocVenda.Linhas.GetEdita(lngLinha).Vendedor.ToUpper()); //PriGlobal: IGNORE
								break;

							}

						}
					} //BID 556890

				}
				else
				{

					objXML.AdicionaCampo("VD", objDocVenda.Responsavel.ToUpper()); //PriGlobal: IGNORE

				}

				objXML.AdicionaCampo("RC", objDocVenda.Responsavel); //PriGlobal: IGNORE
				objXML.AdicionaCampoData("DTUA", objDocVenda.DataUltimaActualizacao); //PriGlobal: IGNORE

				objXML.AdicionaCampo("DEIL", objDocVenda.DE_IL); //PriGlobal: IGNORE

				objXML.AdicionaCampo("PO", objDocVenda.Posto); //PriGlobal: IGNORE
				objXML.AdicionaCampo("UT", objDocVenda.Utilizador); //PriGlobal: IGNORE

				//BID 546595
				if (objDocVenda.Prestacoes.NumItens >= 1)
				{

					dblTotalIva = 0;
					dblTotalRecargo = 0;

					if (objDocVenda.Prestacoes.GetEdita(NumPrest).Valor == 0)
					{

						dblTotalIva = 0;
						dblTotalRecargo = 0;

					}
					else
					{

						if (NumPrest > 1)
						{

							for (int lngNumPrest = 1; lngNumPrest <= NumPrest - 1; lngNumPrest++)
							{

								//BID 575340
								if (objDocVenda.TotalDocumento == 0)
								{

									dblTotalIva = 0;
									dblTotalRecargo = 0;

								}
								else
								{
									//^ BID:575340

									dblTotalIva += TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.TotalIva * objDocVenda.Prestacoes.GetEdita(lngNumPrest).Valor / (objDocVenda.TotalDocumento), objDocVenda.ArredondamentoIva);
									dblTotalRecargo += TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.TotalRecargo * objDocVenda.Prestacoes.GetEdita(lngNumPrest).Valor / (objDocVenda.TotalDocumento), objDocVenda.ArredondamentoIva);

								}

							}

						}

						if (NumPrest == objDocVenda.Prestacoes.NumItens)
						{

							dblTotalIva = objDocVenda.TotalIva - dblTotalIva;
							dblTotalRecargo = objDocVenda.TotalRecargo - dblTotalRecargo;

						}
						else
						{

							dblTotalIva = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.TotalIva * objDocVenda.Prestacoes.GetEdita(NumPrest).Valor / (objDocVenda.TotalDocumento), objDocVenda.ArredondamentoIva);
							dblTotalRecargo = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.TotalRecargo * objDocVenda.Prestacoes.GetEdita(NumPrest).Valor / (objDocVenda.TotalDocumento), objDocVenda.ArredondamentoIva);

						}

					}

					objXML.AdicionaCampoData("DTV", objDocVenda.Prestacoes.GetEdita(NumPrest).DataVenc); //PriGlobal: IGNORE

					if (objDocVenda.GeraPendentePorLinha)
					{ //BID 589198

						objXML.AdicionaCampoNumerico("VT", objDocVenda.Prestacoes.GetEdita(NumPrest).Valor.ToString()); //PriGlobal: IGNORE
						objXML.AdicionaCampoNumerico("TI", dblTotalIva.ToString()); //PriGlobal: IGNORE
						objXML.AdicionaCampoNumerico("TR", dblTotalRecargo.ToString()); //PriGlobal: IGNORE

						//BID 589198
					}
					else
					{

						if (NumPrest < objDocVenda.Prestacoes.NumItens)
						{

							objXML.AdicionaCampoNumerico("VT", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.Prestacoes.GetEdita(NumPrest).Valor) / objDocVenda.TotalDocumento, objDocVenda.Arredondamento).ToString()); //PriGlobal: IGNORE
							objXML.AdicionaCampoNumerico("TI", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.TotalIva * objDocVenda.Prestacoes.GetEdita(NumPrest).Valor / objDocVenda.TotalDocumento) / objDocVenda.TotalDocumento, objDocVenda.ArredondamentoIva).ToString()); //PriGlobal: IGNORE
							objXML.AdicionaCampoNumerico("TR", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.TotalRecargo * objDocVenda.Prestacoes.GetEdita(NumPrest).Valor / objDocVenda.TotalDocumento) / objDocVenda.TotalDocumento, objDocVenda.ArredondamentoIva).ToString()); //PriGlobal: IGNORE

						}
						else
						{

							dblUltValorPrestacao = (dblTotalAdiDoc + objDocVenda.TotalDocumento);
							dblUltValorIva = (dblTotalIvaAdiDoc + objDocVenda.TotalIva);
							dblUltValorRecargo = objDocVenda.TotalRecargo;

							for (int lngNumPrest = 1; lngNumPrest <= NumPrest - 1; lngNumPrest++)
							{

								dblUltValorPrestacao -= TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.Prestacoes.GetEdita(lngNumPrest).Valor) / objDocVenda.TotalDocumento, objDocVenda.Arredondamento); //PriGlobal: IGNORE
								dblUltValorIva -= TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.TotalIva * objDocVenda.Prestacoes.GetEdita(lngNumPrest).Valor / objDocVenda.TotalDocumento) / objDocVenda.TotalDocumento, objDocVenda.ArredondamentoIva); //PriGlobal: IGNORE
								dblUltValorRecargo -= TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda((dblTotalAdiDoc + objDocVenda.TotalDocumento) * (objDocVenda.TotalRecargo * objDocVenda.Prestacoes.GetEdita(lngNumPrest).Valor / objDocVenda.TotalDocumento) / objDocVenda.TotalDocumento, objDocVenda.ArredondamentoIva); //PriGlobal: IGNORE

							}

							objXML.AdicionaCampoNumerico("VT", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblUltValorPrestacao, objDocVenda.Arredondamento).ToString()); //PriGlobal: IGNORE
							objXML.AdicionaCampoNumerico("TI", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblUltValorIva, objDocVenda.ArredondamentoIva).ToString()); //PriGlobal: IGNORE
							objXML.AdicionaCampoNumerico("TR", TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(dblUltValorRecargo, objDocVenda.ArredondamentoIva).ToString()); //PriGlobal: IGNORE

						}

					}
					//Fim 589198

					objXML.AdicionaCampoNumerico("VR", objDocVenda.Prestacoes.GetEdita(NumPrest).TotalRetencao.ToString()); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("VRG", objDocVenda.Prestacoes.GetEdita(NumPrest).TotalRetencaoGarantia.ToString()); //PriGlobal: IGNORE

					//CR.1103
					objXML.AdicionaCampo("ILO", objDocVenda.Prestacoes.GetEdita(NumPrest).IdLinhaOrigem); //PriGlobal: IGNORE
					objXML.AdicionaCampo("DES", objDocVenda.Prestacoes.GetEdita(NumPrest).Descricao); //PriGlobal: IGNORE

					//BID:555541 - Adicionado o IF
					if (objDocVenda.GeraPendentePorLinha)
					{

						//BID:561557 - procura a linha origem do documento de Venda
						int tempForVar2 = objDocVenda.Linhas.NumItens;
						for (int lngLinha = 1; lngLinha <= tempForVar2; lngLinha++)
						{

							if (objDocVenda.Prestacoes.GetEdita(NumPrest).IdLinhaOrigem == objDocVenda.Linhas.GetEdita(lngLinha).IdLinha)
							{

								ObjLinha = objDocVenda.Linhas.GetEdita(lngLinha);
								break;

							}

						}

						if (ObjLinha != null)
						{

							//CS.938
							objXML.AdicionaCampo("OID", ObjLinha.IDObra); //PriGlobal: IGNORE
							objXML.AdicionaCampo("WBSI", ObjLinha.WBSItem); //PriGlobal: IGNORE
							objXML.AdicionaCampo("OIID", ObjLinha.IDItem); //PriGlobal: IGNORE

							ObjLinha = null;
						}

					}
					else
					{

						objXML.AdicionaCampo("OID", objDocVenda.IDObra); //PriGlobal: IGNORE
						objXML.AdicionaCampo("WBSI", objDocVenda.WBSItem); //PriGlobal: IGNORE
						objXML.AdicionaCampo("OIID", ""); //PriGlobal: IGNORE

					}
					//END BID:555541

					strIDLinha = objDocVenda.Prestacoes.GetEdita(NumPrest).IdLinhaOrigem;

					int tempForVar3 = objDocVenda.Linhas.NumItens;
					for (lngNumeroLinha = 1; lngNumeroLinha <= tempForVar3; lngNumeroLinha++)
					{

						if (objDocVenda.Linhas.GetEdita(lngNumeroLinha).IdLinha == strIDLinha)
						{

							break;

						}

					}

				}
				else
				{

					if (lngNumeroLinha == 0)
					{
						lngNumeroLinha = 1;
					} //BID: 557786

					objXML.AdicionaCampoData("DTV", objDocVenda.DataVenc); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("VT", (objDocVenda.TotalDocumento + dblTotalAdiDoc).ToString()); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("TI", (objDocVenda.TotalIva + dblTotalIvaAdiDoc).ToString()); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("TR", objDocVenda.TotalRecargo.ToString()); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("VR", objDocVenda.TotalRetencao.ToString()); //PriGlobal: IGNORE
					objXML.AdicionaCampoNumerico("VRG", objDocVenda.TotalRetencaoGarantia.ToString()); //PriGlobal: IGNORE

					objXML.AdicionaCampo("ILO", ""); //PriGlobal: IGNORE
					objXML.AdicionaCampo("DES", ""); //PriGlobal: IGNORE

					//CS.938
					objXML.AdicionaCampo("OID", objDocVenda.IDObra); //PriGlobal: IGNORE
					objXML.AdicionaCampo("WBSI", objDocVenda.WBSItem); //PriGlobal: IGNORE
					objXML.AdicionaCampo("OIID", ""); //PriGlobal: IGNORE

				}

				dblTotalAdiantamentos = 0;
				dblDifCambio = 0;
				dblCorrecaoMonetaria = 0;
				blnLinhasEstorno = false;

				//CS.3405 - Adiantamentos
				if (NumPrest == 1 && !blnNovosAdiantamentos)
				{

					int tempForVar4 = objDocVenda.Linhas.NumItens;
					for (int lngLinha = 1; lngLinha <= tempForVar4; lngLinha++)
					{

						if (objDocVenda.Linhas.GetEdita(lngLinha).TipoLinha == "90")
						{

							if (Strings.Len(objDocVenda.Linhas.GetEdita(lngLinha).IDHistorico) > 0)
							{

								string tempRefParam = "Select TipoDocumento From DocumentosCCT WITH (NOLOCK) INNER JOIN Historico WITH (NOLOCK) ON Historico.TipoDoc=DocumentosCCT.Documento WHERE Historico.ID='" + objDocVenda.Linhas.GetEdita(lngLinha).IDHistorico + "'";
								Campo = m_objErpBSO.Consulta(tempRefParam);

								if (StringsHelper.ToDoubleSafe(Campo.Valor(0)) == 2)
								{

									if ((StringsHelper.ToDoubleSafe(objDocVenda.Linhas.GetEdita(lngLinha).RegimeIva) == 2 || StringsHelper.ToDoubleSafe(objDocVenda.Linhas.GetEdita(lngLinha).RegimeIva) == 1) && !m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, objDocVenda.Tipodoc, objDocVenda.Serie, "IvaIncluido")) || objDocVenda.Linhas.GetEdita(lngLinha).TotalIva == 0)
									{

										dblTotalAdiantamentos += objDocVenda.Linhas.GetEdita(lngLinha).PrecoLiquido;
										dblAdiantamentoLinha = objDocVenda.Linhas.GetEdita(lngLinha).PrecoLiquido;

									}
									else
									{

										dblTotalAdiantamentos += TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.Linhas.GetEdita(lngLinha).PrecoLiquido * (1 + (objDocVenda.Linhas.GetEdita(lngLinha).TaxaIva / 100)), 2);
										dblAdiantamentoLinha = TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(objDocVenda.Linhas.GetEdita(lngLinha).PrecoLiquido * (1 + (objDocVenda.Linhas.GetEdita(lngLinha).TaxaIva / 100)), 2);

									}

									if (Strings.Len(objDocVenda.Linhas.GetEdita(lngLinha).IdLinhaEstorno) > 0)
									{

										string tempRefParam2 = "SELECT DV1.PagarReceber, DV2.PagarReceber FROM DocumentosVenda DV1 WITH (NOLOCK) LEFT JOIN DocumentosVenda DV2 WITH (NOLOCK) ON DV2.Documento = DV1.DocumentoEstorno WHERE DV1.Documento = '" + objDocVenda.Tipodoc + "'";
										stdEstorno = m_objErpBSO.Consulta(tempRefParam2);

										if (!stdEstorno.Vazia())
										{

											if (stdEstorno.Valor(0) != stdEstorno.Valor(1))
											{

												blnLinhasEstorno = true;

											}

										}

									}

								}

								//Tratamento das diferenças cambiais entre o adiantamento e a venda
								objCampos = (StdBE100.StdBECampos) m_objErpBSO.PagamentosRecebimentos.Historico.DaValorAtributosID(objDocVenda.Linhas.GetEdita(lngLinha).IDHistorico, "Cambio", "CambioMBase", "CambioMalt");
								if (objCampos != null)
								{

									//Apenas vou tratar as diferenças de cambio se existirem cambio distintos
									string tempRefParam3 = "Cambio";
									string tempRefParam4 = "CambioMBase";
									string tempRefParam5 = "CambioMalt";
									if (ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam3).Valor) != objDocVenda.Cambio || ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam4).Valor) != objDocVenda.CambioMBase || ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam5).Valor) != objDocVenda.CambioMAlt)
									{

										string tempRefParam6 = "Cambio";
										string tempRefParam7 = "CambioMBase";
										dblDifCambio += TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(STDPriAPIDivisas.TransfMOrigMEsp(dblAdiantamentoLinha, objDocVenda.Cambio, objDocVenda.CambioMBase, m_objErpBSO.Contexto.MBaseDecArredonda) - STDPriAPIDivisas.TransfMOrigMEsp(dblAdiantamentoLinha, ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam6).Valor), ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam7).Valor), m_objErpBSO.Contexto.MBaseDecArredonda), (short) m_objErpBSO.Contexto.MBaseDecArredonda);
										string tempRefParam8 = "Cambio";
										string tempRefParam9 = "CambioMalt";
										dblCorrecaoMonetaria += TestPrimaveraDarwinSupport.PInvoke.SafeNative.u2lpri.Arredonda(STDPriAPIDivisas.TransfMOrigMEsp(dblAdiantamentoLinha, objDocVenda.Cambio, objDocVenda.CambioMAlt, m_objErpBSO.Contexto.MAltDecArredonda) - STDPriAPIDivisas.TransfMOrigMEsp(dblAdiantamentoLinha, ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam8).Valor), ReflectionHelper.GetPrimitiveValue<double>(objCampos.GetItem(ref tempRefParam9).Valor), m_objErpBSO.Contexto.MAltDecArredonda), (short) m_objErpBSO.Contexto.MAltDecArredonda);

									}

								}

							}

						}

					}

					if (blnLinhasEstorno)
					{

						dblTotalAdiantamentos *= -1;
						dblDifCambio *= -1;
						dblCorrecaoMonetaria *= -1;

					}


				}

				objXML.AdicionaCampoNumerico("TACC", dblTotalAdiantamentos.ToString()); //PriGlobal: IGNORE
				//Diferenças de cambio nos adiantamentos
				objXML.AdicionaCampoNumerico("CORM", dblCorrecaoMonetaria.ToString()); //PriGlobal: IGNORE
				objXML.AdicionaCampoNumerico("DIFC", dblDifCambio.ToString()); //PriGlobal: IGNORE

				if (objDocVenda.GeraPendentePorLinha && objDocVenda.Linhas.NumItens > 0)
				{

					objXML.AdicionaCampo("CIVA", objDocVenda.Linhas.GetEdita(lngNumeroLinha).CodIva); //PriGlobal: IGNORE

				}

				objXML.AdicionaCampo("TIC", ((objDocVenda.TrataIvaCaixa) ? 1 : 0).ToString()); //BID 595111 (foi adicionado o "IIf") 'PriGlobal: IGNORE

				blnIvaIvaIndiferido = false;

				if (objDocVenda.ResumoIva != null)
				{

					if (objDocVenda.ResumoIva.NumItens > 0)
					{

						blnIvaIvaIndiferido = objDocVenda.ResumoIva.GetEdita(1).IVAIndeferido;

					}

				}

				objXML.AdicionaCampo("IVAIND", ((blnIvaIvaIndiferido) ? 1 : 0).ToString()); //BID 595111 (foi adicionado o "IIf") 'PriGlobal: IGNORE

				if (DefCamposUtilHistorico != null)
				{

					string tempRefParam10 = "SELECT Distinct CampoOrigem,CampoDestino FROM LigacaoCamposUtil WITH (NOLOCK) WHERE Operacao = '3' AND TabelaDestino = 'Historico' AND TabelaOrigem = 'CabecDoc' AND (DocumentoOrigem='' OR DocumentoOrigem='" + objDocVenda.Tipodoc + "')";
					objLista = m_objErpBSO.Consulta(tempRefParam10); //PriGlobal: IGNORE

					if (!objLista.NoFim())
					{

						while (!objLista.NoFim())
						{

							//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
							if (!(Convert.IsDBNull(objLista.Valor("CampoDestino")) && Convert.IsDBNull(objLista.Valor("CampoOrigem"))))
							{

								if (objLista.Valor("CampoOrigem").Substring(0, Math.Min(4, objLista.Valor("CampoOrigem").Length)).ToUpper() == "CDU_")
								{

									//UPGRADE_WARNING: (1068) objDocVenda.CamposUtil().Valor of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
									string tempRefParam11 = objLista.Valor("CampoOrigem");
									varValor = ReflectionHelper.GetPrimitiveValue(objDocVenda.CamposUtil.GetItem(ref tempRefParam11).Valor);

								}
								else
								{

									//UPGRADE_WARNING: (1068) CallByName() of type Variant is being forced to Scalar. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
									varValor = ReflectionHelper.GetPrimitiveValue(Interaction.CallByName(objDocVenda, objLista.Valor("CampoOrigem"), CallType.Get));

								}

								//UPGRADE_WARNING: (1049) Use of Null/IsNull() detected. More Information: http://www.vbtonet.com/ewis/ewi1049.aspx
								if (!Convert.IsDBNull(varValor))
								{


									switch(DefCamposUtilHistorico[objLista.Valor("CampoDestino")].TipoSimplificado)
									{
										case StdBE100.StdBETipos.EnumTipoCampoSimplificado.tsBooleano : 
											 
											objXML.AdicionaCampoLogico(objLista.Valor("CampoDestino").ToUpper(), ReflectionHelper.GetPrimitiveValue<bool>(varValor)); 
											 
											break;
										case StdBE100.StdBETipos.EnumTipoCampoSimplificado.tsData : 
											 
											objXML.AdicionaCampoData(objLista.Valor("CampoDestino").ToUpper(), ReflectionHelper.GetPrimitiveValue<System.DateTime>(varValor)); 
											 
											break;
										case StdBE100.StdBETipos.EnumTipoCampoSimplificado.tsDouble : case StdBE100.StdBETipos.EnumTipoCampoSimplificado.tsMonetario : case StdBE100.StdBETipos.EnumTipoCampoSimplificado.tsSingle : 
											 
											objXML.AdicionaCampoNumerico(objLista.Valor("CampoDestino").ToUpper(), ReflectionHelper.GetPrimitiveValue<string>(varValor)); 
											 
											break;
										default:
											 
											objXML.AdicionaCampo(objLista.Valor("CampoDestino").ToUpper(), ReflectionHelper.GetPrimitiveValue<string>(varValor)); 
											 
											break;
									}

								}

							}

							objLista.Seguinte();

						}

					}


				}

				objXML.AdicionaNodo(" /> "); // & vbNewLine


				stdEstorno = null;
				ObjLinha = null;
				Campo = null;
				objLista = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.PreencheXMLHistorico", excep.Message);
			}
		}

		public string DaIDLinhaATransformar(string IdLinha)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaIDLinhaATransformar(ref IdLinha);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaIDLinhaATransformar", excep.Message);
			}
			return "";
		}

		public void ActualizaValorAtributoID(string Id, string Atributo, dynamic Valor)
		{

			StdBE100.StdBECampos strAtributos = null;

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (Erro)");

Inicio:

			m_objErpBSO.IniciaTransaccao();

			dynamic[] tempRefParam = new dynamic[]{"Assinatura"};
			strAtributos = m_objErpBSO.Vendas.Documentos.DaValorAtributosID(Id, tempRefParam);

			string tempRefParam2 = "1";
			if (Strings.Len(ReflectionHelper.GetPrimitiveValue<string>(strAtributos.GetItem(ref tempRefParam2).Valor)) > 0)
			{

				//Valida se o atributo é passível de ser alterado
				CertificacaoSoftware.ValidaAtributoAlteravel(Atributo);

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributoID(ref Id, ref Atributo, ref Valor);

			}
			else
			{

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributoID(ref Id, ref Atributo, ref Valor);

			}

			m_objErpBSO.TerminaTransaccao();
			return;

Erro:
			m_objErpBSO.DesfazTransaccao();
			if (m_objErpBSO.VerificaErroLock())
			{
				//UPGRADE_TODO: (1065) Error handling statement (Inicio) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
				UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("Resume Label (Inicio)");
			}
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaValorAtributoID", Information.Err().Description); //PriGlobal: IGNORE
		}

		public void ActualizaValorAtributosID(string Id, StdBECampos Atributos)
		{

			string Atributo = "";
			StdBE100.StdBECampos strAtributos = null;

			//UPGRADE_TODO: (1065) Error handling statement (On Error Goto) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
			UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("On Error Goto Label (Erro)");

			if ((Atributos == null))
			{
				StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.ActualizaValorAtributosID", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(8868, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
			}
			if ((Atributos.NumItens == 0))
			{
				return;
			}

Inicio:

			m_objErpBSO.IniciaTransaccao();

			dynamic[] tempRefParam = new dynamic[]{"Assinatura"};
			strAtributos = m_objErpBSO.Vendas.Documentos.DaValorAtributosID(Id, tempRefParam);

			string tempRefParam2 = "1";
			if (Strings.Len(ReflectionHelper.GetPrimitiveValue<string>(strAtributos.GetItem(ref tempRefParam2).Valor)) > 0)
			{

				int tempForVar = Atributos.NumItens;
				for (int lngAtr = 1; lngAtr <= tempForVar; lngAtr++)
				{

					string tempRefParam3 = lngAtr.ToString();
					Atributo = Atributos.GetItem(ref tempRefParam3).Nome;
					lngAtr = Convert.ToInt32(Double.Parse(tempRefParam3));

					//Valida se o atributo é passível de ser alterado
					CertificacaoSoftware.ValidaAtributoAlteravel(Atributo);

				}

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributosID(ref Id, ref Atributos);

			}
			else
			{

				m_objErpBSO.DSO.Vendas.Documentos.ActualizaValorAtributosID(ref Id, ref Atributos);

			}

			m_objErpBSO.TerminaTransaccao();

			return;

Erro:
			m_objErpBSO.DesfazTransaccao();
			if (m_objErpBSO.VerificaErroLock())
			{
				//UPGRADE_TODO: (1065) Error handling statement (Inicio) could not be converted. More Information: http://www.vbtonet.com/ewis/ewi1065.aspx
				UpgradeHelpers.Helpers.NotUpgradedHelper.NotifyNotUpgradedElement("Resume Label (Inicio)");
			}
			//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
			StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.ActualizaValorAtributosID", Information.Err().Description); //PriGlobal: IGNORE
		}

		public string DaDocDestinoEstorno(string IdCabec)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaDocDestinoEstorno(ref IdCabec);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaDocDestinoEstorno", excep.Message);
			}
			return "";
		}


		// No caso de a factura ser gerada pela aplicação gabinetes,
		//  este procedimento serve para efectuar o estorno de todos os custos e avenças,
		//  processadas na aplicação origem.
		// Recebe como parâmetro o Id que identifica o documento a anular na GCP,
		//  e devolve em caso de sucesso uma mensagem, com a indicação de quais os custos
		//  que foram estornados com sucesso: Custos e/ou Avenças
		private void EstornaDocumentosGabinetes(string IdDocGCP, ref string strAvisos)
		{

			try
			{


				if (Strings.Len(IdDocGCP) == 0)
				{
					return;
				}

				// Não deve verificar a licença. Também deve funcionar em modo demonstração.
				//If m_objErpBSO.Licenca.SubmoduloDemo("SPR.BAS") Then Exit Sub

				// Não é necessário estornar aqui os custos pois o estorno da factura já faz isso.
				//If m_objErpBSO.Gabinetes.CadastroCustos.EstornaCustoFacturado(IdDocGCP) Then
				//    strAvisos = strAvisos & m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13610, FuncoesComuns100.ModuloGCP) & vbCrLf
				//End If

				if (Convert.ToBoolean(m_objErpBSO.Gabinetes.Facturacao.EstornaFacturacao(IdDocGCP)))
				{
					strAvisos = strAvisos + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13610, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
					strAvisos = strAvisos + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(13611, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.EstornaDocumentosGabinetes", excep.Message);
			}

		}

		//BID 535984
		public double DaTotalDocumento(string strIdCabec)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaTotalDocumento(ref strIdCabec);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas.DaTotalDocumento", excep.Message);
			}
			return 0;
		}
		//^BID 535984

		//CR.1103
		public double DaTotalDocumentoEX(string strIdCabec, string strCodIva)
		{

			try
			{


				return m_objErpBSO.DSO.Vendas.Documentos.DaTotalDocumentoEX(ref strIdCabec, ref strCodIva);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas.DaTotalDocumentoEX", excep.Message);
			}
			return 0;
		}
		//^CR.1103

		//CR.781
		private void ReactivaLotes(VndBE100.VndBEDocumentoVenda objDocVenda, VndBE100.VndBETabVenda objTabVenda)
		{

			try
			{

				if (!objTabVenda.LigacaoStocks)
				{
					return;
				}

				int tempForVar = objDocVenda.Linhas.NumItens;
				VndBE100.VndBELinhaDocumentoVenda withVar = null;
				for (int lngLinha = 1; lngLinha <= tempForVar; lngLinha++)
				{
					withVar = objDocVenda.Linhas.GetEdita(lngLinha);
					if (((objTabVenda.TipoMovStock == "E") && (withVar.Quantidade >= 0)) || ((objTabVenda.TipoMovStock == "S") && (withVar.Quantidade < 0)))
					{
						if ((Strings.Len(withVar.Artigo) > 0) && (Strings.Len(withVar.Lote) > 0) && (withVar.Lote != ConstantesPrimavera100.Inventario.LotePorDefeito))
						{
							if (~Convert.ToInt32(m_objErpBSO.Inventario.ArtigosLotes.DaLoteActivo(withVar.Artigo, withVar.Lote)) != 0)
							{
								m_objErpBSO.Inventario.ArtigosLotes.ActualizaValorAtributo(withVar.Artigo, withVar.Lote, "Activo", 1);
							}
						}
					}
				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_ReactivaLotes", excep.Message);
			}

		}
		//^CR.781

		//CS.507 7.50_RC1 - Este método foi retirado da interface para passar para os motores
		public VndBE100.VndBEDocumentoVenda EstornaDocumentoVenda(string IDDocumentoOrigem, string MotivoEstorno, string Observacoes, System.DateTime DataDocumentoEstorno, System.DateTime DataIntroducao, VndBE100.VndBEDocumentoVenda DocumentoEstorno, bool GravaDocumentoEstorno, string Avisos)
		{
			//----------------------------
			VndBE100.VndBEDocumentoVenda result = null;
			VndBE100.VndBEDocumentoVenda objDocNovo = null;
			VndBE100.VndBEDocumentoVenda objDocOrigem = null;
			bool blnLigaCC = false;
			//----------------------------

			try
			{

				objDocOrigem = m_objErpBSO.Vendas.Documentos.EditaID(IDDocumentoOrigem);

				//BID:543143
				if (objDocOrigem != null)
				{
					if (objDocOrigem.Fechado || objDocOrigem.Anulado || (objDocOrigem.Estado == ConstantesPrimavera100.Documentos.EstadoDocTransformado))
					{
						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.EstornaDocumentoVenda", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15382, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
					}
				}
				else
				{
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.EstornaDocumentoVenda", m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(15383, FuncoesComuns100.InterfaceComunsUS.ModuloGCP));
				}
				//END BID:543143

				//Chama o método que vai preencher um documento de estorno e um novo documento, caso o motivo crie novos documentos
				dynamic tempRefParam = DocumentoEstorno;
				dynamic tempRefParam2 = objDocNovo;
				FuncoesComuns100.FuncoesBS.Documentos.EfectuaEstornoDocumentos(ref MotivoEstorno, ref Observacoes, objDocOrigem, ref tempRefParam, ref tempRefParam2, ConstantesPrimavera100.Modulos.Vendas, ref DataDocumentoEstorno, ref DataIntroducao);
				objDocNovo = (VndBE100.VndBEDocumentoVenda) tempRefParam2;
				DocumentoEstorno = (VndBE100.VndBEDocumentoVenda) tempRefParam;

				//Se o utilizador desejar gravar já o documento de estorno, estão este será gravado..
				if (GravaDocumentoEstorno)
				{

					//BID 582582
					m_objErpBSO.Vendas.Documentos.CalculaValoresTotais(ref DocumentoEstorno);

					string tempRefParam3 = DocumentoEstorno.Tipodoc;
					string tempRefParam4 = "LigaCC";
					blnLigaCC = m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam3, tempRefParam4));

					//Faz o tratamento das restantes questões relacionadas com os Documentos de venda..
					if (blnLigaCC)
					{


						//BID 582582
						//Retenções..
						//                Set .Retencoes = m_objErpBSO.Vendas.Documentos.CalculaRetencoes(DocumentoEstorno)
						//                .TotalRetencao = m_objErpBSO.PagamentosRecebimentos.Pendentes.FuncoesBS.Documentos.CalculaTotalRetencao(.Retencoes)
						//                .TotalRetencaoGarantia = m_objErpBSO.PagamentosRecebimentos.Pendentes.FuncoesBS.Documentos.CalculaTotalRetencaoGarantia(.Retencoes)

						DocumentoEstorno.Retencoes = objDocOrigem.Retencoes;

						if (DocumentoEstorno.Retencoes != null)
						{

							int tempForVar = DocumentoEstorno.Retencoes.NumItens;
							for (int intI = 1; intI <= tempForVar; intI++)
							{

								if ((DocumentoEstorno.TotalDocumento < 0 && DocumentoEstorno.Retencoes.GetEdita(intI).Incidencia > 0) || (DocumentoEstorno.TotalDocumento > 0 && DocumentoEstorno.Retencoes.GetEdita(intI).Incidencia < 0))
								{

									DocumentoEstorno.Retencoes.GetEdita(intI).Incidencia = (-1) * DocumentoEstorno.Retencoes.GetEdita(intI).Incidencia;
									DocumentoEstorno.Retencoes.GetEdita(intI).Valor = (-1) * DocumentoEstorno.Retencoes.GetEdita(intI).Valor;

								}

							}

						}

						if (DocumentoEstorno.Retencoes != null)
						{

							if (DocumentoEstorno.Retencoes.NumItens > 0)
							{

								DocumentoEstorno.TotalRetencao = Convert.ToDouble(m_objErpBSO.PagamentosRecebimentos.Pendentes.CalculaTotalRetencao(DocumentoEstorno.Retencoes));
								DocumentoEstorno.TotalRetencaoGarantia = Convert.ToDouble(m_objErpBSO.PagamentosRecebimentos.Pendentes.CalculaTotalRetencaoGarantia(DocumentoEstorno.Retencoes));

							}

						}

						//Prestações..
						DocumentoEstorno.Prestacoes = objDocOrigem.Prestacoes;


					}

					string tempRefParam5 = "";
					string tempRefParam6 = "";
					m_objErpBSO.Vendas.Documentos.Actualiza(DocumentoEstorno, Avisos, tempRefParam5, tempRefParam6);

				}

				//Retorna o novo documento
				result = objDocNovo;

				objDocNovo = null;
				objDocOrigem = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.EstornaDocumentoVenda", excep.Message);
			}

			return result;
		}

		public VndBE100.VndBEDocumentoVenda EstornaDocumentoVenda( string IDDocumentoOrigem,  string MotivoEstorno,  string Observacoes,  System.DateTime DataDocumentoEstorno,  System.DateTime DataIntroducao,  VndBE100.VndBEDocumentoVenda DocumentoEstorno,  bool GravaDocumentoEstorno)
		{
			string tempRefParam493 = "";
			return EstornaDocumentoVenda( IDDocumentoOrigem,  MotivoEstorno,  Observacoes,  DataDocumentoEstorno,  DataIntroducao,  DocumentoEstorno,  GravaDocumentoEstorno,  tempRefParam493);
		}

		public VndBE100.VndBEDocumentoVenda EstornaDocumentoVenda( string IDDocumentoOrigem,  string MotivoEstorno,  string Observacoes,  System.DateTime DataDocumentoEstorno,  System.DateTime DataIntroducao,  VndBE100.VndBEDocumentoVenda DocumentoEstorno)
		{
			bool tempRefParam494 = true;
			string tempRefParam495 = "";
			return EstornaDocumentoVenda( IDDocumentoOrigem,  MotivoEstorno,  Observacoes,  DataDocumentoEstorno,  DataIntroducao,  DocumentoEstorno,  tempRefParam494,  tempRefParam495);
		}
		//END CS.507 7.50_RC1

		//CS.508 - 7.50_RC1: Passagem das transformações para os motores
		public StdBELista LstTiposDocumentosParaTransformacao(string TipoDocumento)
		{

			try
			{

				bool tempRefParam = m_objErpBSO.Vendas.Licenca.Vendas.Encomendas;

				return m_objErpBSO.DSO.Vendas.Documentos.LstTiposDocumentosParaTransformacao(ref TipoDocumento, ref tempRefParam);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_LstTiposDocumentosParaTransformacao", excep.Message);
			}
			return null;
		}

		public void TransformaDocumento(dynamic[] Documentos, VndBEDocumentoVenda DocumentoDestino, bool PreencheDadosRelacionados, string Avisos, bool GravaDocumento)
		{
			//---------------------------------------
			string strValidacao = "";
			BasBETipos.LOGTipoDocumento intTipoDoc = BasBETipos.LOGTipoDocumento.LOGDocPedidoCotacao;
			string strIdPaiOriginal = "";
			string strIdPaiNovo = "";
			string strEstadoOrigem = "";
			string strEstadoDestino = "";
			InvBE100.InvBETipos.EnumTipoConfigEstados intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;
			string strErroEstado = "";


			try
			{

				if (PreencheDadosRelacionados)
				{

					short tempRefParam = -1;
					DocumentoDestino = m_objErpBSO.Vendas.Documentos.PreencheDadosRelacionados(DocumentoDestino, tempRefParam);
					DocumentoDestino.DataVenc = m_objErpBSO.Base.CondsPagamento.CalculaDataVencimento(DocumentoDestino.DataDoc, DocumentoDestino.CondPag, 0, DocumentoDestino.TipoEntidade, DocumentoDestino.Entidade);

				}

				strValidacao = FuncoesComuns100.FuncoesBS.Documentos.ValidaTransformacaoDocumentos(ArraysHelper.CastArray<dynamic[]>(Documentos), DocumentoDestino, ConstantesPrimavera100.Modulos.Vendas);
				if (Strings.Len(strValidacao) > 0)
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_TransformaDocumento", strValidacao);

				}

				//UPGRADE_WARNING: (6021) Casting 'int' to Enum may cause different behaviour. More Information: http://www.vbtonet.com/ewis/ewi6021.aspx
				string tempRefParam2 = DocumentoDestino.Tipodoc;
				string tempRefParam3 = "TipoDocumento";
				intTipoDoc = (BasBETipos.LOGTipoDocumento) m_objErpBSO.DSO.Plat.Utils.FInt(m_objErpBSO.Vendas.TabVendas.DaValorAtributo(tempRefParam2, tempRefParam3));

				//Percorre a lista de documentos de origem e copia os dados para o documento de destino
				foreach (dynamic Documentos_item in Documentos)
				{

					foreach (dynamic objDocLinha in (IEnumerable) Documentos_item.Linhas)
					{


						objDocLinha.EstadoBD = BasBETiposGcp.enuEstadosBD.estNovo;
						objDocLinha.IDLinhaOriginal = objDocLinha.IdLinha;
						bool tempRefParam4 = true;
						objDocLinha.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam4);

						if (m_objErpBSO.DSO.Plat.Utils.FInt(ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(objDocLinha.Artigo, "TratamentoDim")) == 1) != 0)
						{

							strIdPaiOriginal = objDocLinha.IDLinhaOriginal;
							strIdPaiNovo = objDocLinha.IdLinha;

						}

						if (objDocLinha.IdLinhaPai == strIdPaiOriginal)
						{

							objDocLinha.IdLinhaPai = strIdPaiNovo;

						}

						objDocLinha.Quantidade -= objDocLinha.QuantSatisfeita;
						objDocLinha.QuantSatisfeita = 0;

						if (objDocLinha.Quantidade < 0)
						{

							objDocLinha.PrecUnit = Math.Abs(objDocLinha.PrecUnit);

						}

						if ((m_objErpBSO.DSO.Plat.Utils.FInt(objDocLinha.TipoLinha) >= 10 && m_objErpBSO.DSO.Plat.Utils.FInt(objDocLinha.TipoLinha) <= 30) || m_objErpBSO.DSO.Plat.Utils.FInt(objDocLinha.TipoLinha) == 91)
						{

							if (!objDocLinha.Devolucao)
							{

								objDocLinha.PCMDevolucao = 0;

							}

							if (Strings.Len(m_objErpBSO.DSO.Plat.Utils.FStr(objDocLinha.DataEntrega)) == 0 && intTipoDoc == BasBETipos.LOGTipoDocumento.LOGDocEncomenda)
							{

								objDocLinha.DataEntrega = DateTime.Parse(DocumentoDestino.DataDoc.AddDays(ReflectionHelper.GetPrimitiveValue<double>(m_objErpBSO.Base.Artigos.DaValorAtributo(objDocLinha.Artigo, "PrazoEntrega"))).ToString("d"));

							}

							if (objDocLinha.MovStock == "S")
							{

								objDocLinha.DataStock = DateTime.Parse(m_objErpBSO.DSO.Plat.Utils.FStr(DocumentoDestino.DataDoc) + " " + DateTimeHelper.Time.ToString("HH:mm:SS"));

							}

						}

						//Historico de Residuos e IEC
						objDocLinha.LinhasHistoricoResiduo.RemoveTodos();
						objDocLinha.LinhasHistoricoIEC.RemoveTodos();

						FuncoesComuns100.FuncoesBS.Documentos.PreencheHistoricoIEC(objDocLinha, ConstantesPrimavera100.Modulos.Vendas);
						PreencheHistoricoResiduos(objDocLinha, DocumentoDestino.LocalOperacao);

						if (objDocLinha.MovStock == "S")
						{

							if (objDocLinha.Quantidade >= 0)
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovPositivos;

							}
							else
							{

								intTipoMov = InvBE100.InvBETipos.EnumTipoConfigEstados.configMovNegativos;

							}

							//Se o documento origem e destino têm naturezas inversas vamos encontrar os estados de estorno(caso existam)
							if (FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, Convert.ToString(Documentos_item.TipoDoc)) != FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, DocumentoDestino.Tipodoc))
							{

								FuncoesComuns100.FuncoesBS.Documentos.PreencheEstadosInventarioLinhaEstorno(DocumentoDestino.Tipodoc, objDocLinha);

							}
							else
							{

								if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaEstadosInventarioLinha(1, DocumentoDestino.Tipodoc, objDocLinha, ref strErroEstado))
								{

									m_objErpBSO.Inventario.ConfiguracaoEstados.DevolveEstadoDefeito(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas), DocumentoDestino.Tipodoc, intTipoMov, strEstadoOrigem, strEstadoDestino);
									objDocLinha.INV_EstadoOrigem = strEstadoOrigem;
									objDocLinha.INV_EstadoDestino = strEstadoDestino;

								}

							}

						}

						//Insere a linha no documento de destino
						if (objDocLinha.Quantidade != 0 || objDocLinha.TipoLinha == "60")
						{

							DocumentoDestino.Linhas.Insere(objDocLinha);

						}


					}

				}

				if (GravaDocumento)
				{

					//Actualiza o documento de Venda
					string tempRefParam5 = "";
					string tempRefParam6 = "";
					m_objErpBSO.Vendas.Documentos.Actualiza(DocumentoDestino, Avisos, tempRefParam5, tempRefParam6);

				}
			}
			catch (System.Exception excep)
			{

				DocumentoDestino = null;
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_TransformaDocumento", excep.Message);
			}

		}

		public void TransformaDocumento( dynamic[] Documentos,  VndBEDocumentoVenda DocumentoDestino,  bool PreencheDadosRelacionados,  string Avisos)
		{
			bool tempRefParam496 = true;
			TransformaDocumento( Documentos,  DocumentoDestino,  PreencheDadosRelacionados,  Avisos,  tempRefParam496);
		}

		public void TransformaDocumento( dynamic[] Documentos,  VndBEDocumentoVenda DocumentoDestino,  bool PreencheDadosRelacionados)
		{
			string tempRefParam497 = "";
			bool tempRefParam498 = true;
			TransformaDocumento( Documentos,  DocumentoDestino,  PreencheDadosRelacionados,  tempRefParam497,  tempRefParam498);
		}

		public void TransformaDocumento( dynamic[] Documentos,  VndBEDocumentoVenda DocumentoDestino)
		{
			bool tempRefParam499 = false;
			string tempRefParam500 = "";
			bool tempRefParam501 = true;
			TransformaDocumento( Documentos,  DocumentoDestino,  tempRefParam499,  tempRefParam500,  tempRefParam501);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : FuncoesComuns100.FuncoesBS.Utils.BloqueiaRegistosDocumentoVenda
		// Description : Bloqueia registos para actualização de um Documentos de Venda
		// Arguments   : clsDocumentoVenda --> Documento de Venda
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void BloqueiaRegistosDocumentoVenda(VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{
			VndBE100.VndBELinhaDocumentoVenda ObjLinha = null;
			string strEntidades = "";
			string strSeries = "";
			StdBE100.StdBEStringBuilder objArtigos = null;
			StdBE100.StdBEStringBuilder objLock = null;
			string strArtigo = "";

			try
			{


				//Bloqueio para entidade
				if (Strings.Len(clsDocumentoVenda.Entidade) > 0)
				{

					strEntidades = FuncoesComuns100.FuncoesBS.Entidades.BloqueiaEntidadeSQL(clsDocumentoVenda.TipoEntidadeFac, clsDocumentoVenda.EntidadeFac);
				}

				//Bloqueio para séries
				if (!clsDocumentoVenda.EmModoEdicao)
				{

					string tempRefParam = "SeriesVendas";
					strSeries = strSeries + FuncoesComuns100.FuncoesBS.Utils.BloqueiaRegistos(ref tempRefParam, "TipoDoc", clsDocumentoVenda.Tipodoc, "Serie", clsDocumentoVenda.Serie) + "\r"; //PriGlobal: IGNORE
				}

				objArtigos = new StdBE100.StdBEStringBuilder();
				objLock = new StdBE100.StdBEStringBuilder();

				//Bloqueio dos Artigos ou Artigos Armazém
				foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha2 in clsDocumentoVenda.Linhas)
				{
					ObjLinha = ObjLinha2;


					//Se a linha tem artigo
					if (Strings.Len(ObjLinha.Artigo) != 0)
					{

						string tempRefParam2 = "'@1@'";
						dynamic[] tempRefParam3 = new dynamic[]{ObjLinha.Artigo};
						strArtigo = m_objErpBSO.DSO.Plat.Sql.FormatSQL(tempRefParam2, tempRefParam3);

						if ((objArtigos.Value().IndexOf(strArtigo) + 1) == 0)
						{

							//Cria string para bloqueio para artigo
							if (Strings.Len(objArtigos.Value()) != 0)
							{

								objArtigos.Append(" , ");
							}
							else
							{

								objArtigos.Append("(");
							}

							objArtigos.Append(strArtigo);
						}

					}
					ObjLinha = null;
				}


				//**** PARA EVITAR DEADLOCKS TEMOS DE BLOQUEAR PRIMEIRO OS ARTIGOS/ARTIGOS ARMAZÉM ***
				//**** O BLOQUEIO DEVE COMEÇAR PELAS TABELAS COM MAIOR REGISTOS (A INSERIR/ACTUALIZAR/APAGAR) ***

				//Cria string para bloqueio para artigo
				if (Strings.Len(objArtigos.Value()) != 0)
				{

					string tempRefParam4 = "Artigo";
					string tempRefParam5 = "Artigo IN " + objArtigos.Value() + ")";
					objLock.Append(m_objErpBSO.BloqueiaRegistosSQL(tempRefParam4, tempRefParam5) + "\r"); //PriGlobal: IGNORE
				}

				//Cria string para bloqueio para entidades
				if (Strings.Len(strEntidades) != 0)
				{

					objLock.Append(strEntidades + "\r");
				}

				//Cria string para bloqueio para series
				if (Strings.Len(strSeries) != 0)
				{

					objLock.Append(strSeries + "\r");
				}

				//Bloqueio registos
				if (Strings.Len(objLock.Value()) != 0)
				{

					DbCommand TempCommand = null;
					TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
					UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
					TempCommand.CommandText = objLock.Value();
					UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
					TempCommand.ExecuteNonQuery();
				}


				objArtigos = null;
				objLock = null;
				ObjLinha = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_BloqueiaRegistosDocumentoVenda", excep.Message);
			}

		}

		//CS.3419
		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs, System.DateTime DataInicial, System.DateTime DataFinal, string TiposLancamento, string SQLCamposSelect, bool ComunicacaoWSAT)
		{
			StdBELista result = null;
			dynamic objSAFT = null;
			string strSQLWhere = "";

			try
			{

				//Data Inicial
				//UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
				if (DataInicial.Equals(DateTime.FromOADate(0)) || (DataInicial == DateTime.FromOADate(0)))
				{

					//BID 582419
					if (ComunicacaoWSAT)
					{

						DataInicial = DateAndTime.DateSerial(2013, 1, 1);

					}
					else
					{
						//Fim 582419

						DataInicial = DateTime.Today.AddMonths(-1);
						DataInicial = DateAndTime.DateSerial(DataInicial.Year, DataInicial.Month, 1);

						if (DataInicial.Year < 2013)
						{

							DataInicial = DateAndTime.DateSerial(2013, 1, 1);

						}

					}

				}

				//Data Final
				//UPGRADE_WARNING: (2080) IsEmpty was upgraded to a comparison and has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2080.aspx
				if (DataFinal.Equals(DateTime.FromOADate(0)) || (DataFinal == DateTime.FromOADate(0)))
				{

					DataFinal = DateTime.Today;

				}

				//Obtém a lista
				objSAFT = FuncoesComuns100.FuncoesBS.Utils.DaPrimaveraSaftPT();


				if (ComunicacaoWSAT)
				{

					strSQLWhere = "SV.TipoComunicacao = @1@"; //PriGlobal: IGNORE
					dynamic[] tempRefParam = new dynamic[]{(int) BasBETiposGcp.EnumSerieTipoComunicacao.ViaWebService};
					strSQLWhere = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQLWhere, tempRefParam);

					result = (StdBELista) objSAFT.ListaDocsExportacaoSAFT_Cabec(ATEstadoDocs, DataInicial, DataFinal, TiposLancamento, strSQLWhere);

				}
				else
				{

					//Campos a devolver na lista
					if (Strings.Len(SQLCamposSelect) == 0)
					{

						SQLCamposSelect = "*";

					}

					result = (StdBELista) objSAFT.ListaDocsExportacaoSAFT(ATEstadoDocs, DataInicial, DataFinal, TiposLancamento, SQLCamposSelect);

				}


				objSAFT = null;
			}
			catch (System.Exception excep)
			{

				//Set objSAFT = Nothing
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_ListaDocsExportacaoSAFT", excep.Message);
			}

			return result;
		}

		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs, System.DateTime DataInicial, System.DateTime DataFinal, string TiposLancamento, string SQLCamposSelect)
		{
			return ListaDocsExportacaoSAFT(ATEstadoDocs, DataInicial, DataFinal, TiposLancamento, SQLCamposSelect, false);
		}

		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs, System.DateTime DataInicial, System.DateTime DataFinal, string TiposLancamento)
		{
			return ListaDocsExportacaoSAFT(ATEstadoDocs, DataInicial, DataFinal, TiposLancamento, "", false);
		}

		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs, System.DateTime DataInicial, System.DateTime DataFinal)
		{
			return ListaDocsExportacaoSAFT(ATEstadoDocs, DataInicial, DataFinal, "", "", false);
		}

		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs, System.DateTime DataInicial)
		{
			return ListaDocsExportacaoSAFT(ATEstadoDocs, DataInicial, DateTime.Parse("12:00:00 AM"), "", "", false);
		}

		public StdBELista ListaDocsExportacaoSAFT(EnumATEstadoDocs ATEstadoDocs)
		{
			return ListaDocsExportacaoSAFT(ATEstadoDocs, DateTime.Parse("12:00:00 AM"), DateTime.Parse("12:00:00 AM"), "", "", false);
		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_ActualizaEstadoEnvioAT
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		public void ActualizaEstadoEnvioAT(string IdCabecDoc, EnumATTrataTrans ATTrataTrans)
		{

			m_objErpBSO.DSO.Vendas.Documentos.ActualizaEstadoEnvioAT(IdCabecDoc, ATTrataTrans);

		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_ActualizaATDocCodeID
		// Description   :
		// Arguments     :
		// Returns       : None
		//---------------------------------------------------------------------------------------
		public void ActualizaATDocCodeID(string IdCabecDoc, string Valor)
		{
			string strErros = "";

			//Valida a actualização deste valor
			if (!FuncoesComuns100.FuncoesBS.Documentos.ValidaActualizacaoATDocCodeID(ConstantesPrimavera100.Modulos.Vendas, IdCabecDoc, Valor, ref strErros))
			{

				StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_IVndBSVendas_ActualizaATDocCodeID", strErros);

			}

			//Actualiza o valor
			m_objErpBSO.DSO.Vendas.Documentos.ActualizaATDocCodeID(IdCabecDoc, Valor);

		}

		//---------------------------------------------------------------------------------------
		// Procedure     : IVndBSVendas_ImportaAutofacturacaoSAFT
		// Description   :
		// Arguments     :
		// Returns       : Boolean
		//---------------------------------------------------------------------------------------
		public bool ImportaAutofacturacaoSAFT(string xmlSAFT, string tmpCabec, string tmpDetail, string strErros)
		{
			string strSQL = "";
			int lOpenFile = 0;
			string sFileText = "";

			try
			{

				lOpenFile = FileSystem.FreeFile();
				FileSystem.FileOpen(lOpenFile, xmlSAFT, OpenMode.Input, OpenAccess.Default, OpenShare.Default, -1);
				sFileText = FileSystem.InputString(lOpenFile, (int) FileSystem.LOF(lOpenFile));
				FileSystem.FileClose(lOpenFile);

				//Substitui 'Xlmns schema' para 'AuditFile'
				sFileText = Strings.Replace(sFileText, "<AuditFile xmlns", "<AuditFile>", 1, -1, CompareMethod.Binary); //PriGlobal: IGNORE
				sFileText = Strings.Replace(sFileText, "'", "''", 1, -1, CompareMethod.Binary);

				if (Strings.Len(tmpCabec) == 0)
				{

					tmpCabec = "#tmpcabec" + FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaPostoComSessao(); //PriGlobal: IGNORE

				}

				if (Strings.Len(tmpDetail) == 0)
				{

					tmpDetail = "#tmpDetail" + FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaPostoComSessao(); //PriGlobal: IGNORE

				}

				strSQL = "  EXEC Std_DropTempTable '@1@'" + "\r";
				strSQL = strSQL + "  EXEC Std_DropTempTable '@2@'" + "\r";

				//Cabeçalho
				strSQL = strSQL + "DECLARE @xmlImport as XML" + "\r";
				strSQL = strSQL + " DECLARE @intIdentificador AS INT" + "\r";
				strSQL = strSQL + " SELECT @xmlImport = '@3@'" + "\r";
				strSQL = strSQL + " EXEC sp_xml_preparedocument @intIdentificador OUTPUT, @xmlImport" + "\r";
				strSQL = strSQL + " SELECT * INTO @1@ FROM OPENXML (@intIdentificador, 'AuditFile/SourceDocuments/SalesInvoices/Invoice',2)" + "\r";
				strSQL = strSQL + " WITH (InvoiceNo nvarchar(100) 'InvoiceNo'," + "\r";
				strSQL = strSQL + " InvoiceDate Datetime 'InvoiceDate'," + "\r";
				strSQL = strSQL + " NetTotal float 'DocumentTotals/NetTotal'," + "\r";
				strSQL = strSQL + " TaxPayable float 'DocumentTotals/TaxPayable'," + "\r";
				strSQL = strSQL + " GrossTotal float 'DocumentTotals/GrossTotal'," + "\r";
				strSQL = strSQL + " Hash nvarchar(500) 'Hash'," + "\r";
				strSQL = strSQL + " HashControl nvarchar(10) 'HashControl'," + "\r";
				strSQL = strSQL + " CustomerID nvarchar(100) 'CustomerID')";

				//Detalhe

				strSQL = strSQL + " SET @intIdentificador =  0" + "\r";
				strSQL = strSQL + " EXEC sp_xml_preparedocument @intIdentificador OUTPUT, @xmlImport" + "\r";
				strSQL = strSQL + " SELECT * INTO @2@ FROM OPENXML (@intIdentificador, 'AuditFile/SourceDocuments/SalesInvoices/Invoice/Line',2)" + "\r";
				strSQL = strSQL + " WITH (InvoiceNo nvarchar(100) '../InvoiceNo'," + "\r";
				strSQL = strSQL + " ProductCode nvarchar(100) 'ProductCode'," + "\r";
				strSQL = strSQL + " ProductDescription nvarchar(100) 'ProductDescription'," + "\r";
				strSQL = strSQL + " Quantity int 'Quantity'," + "\r";
				strSQL = strSQL + " UnitOfMeasure nvarchar(10) 'UnitOfMeasure'," + "\r";
				strSQL = strSQL + " UnitPrice float 'UnitPrice')" + "\r";
				strSQL = strSQL + " Alter Table @2@ ADD uid UniqueIdentifier NOT NULL default newid(), " + "\r";
				strSQL = strSQL + " ArtigoSugerido nvarchar(50) NULL, " + "\r";
				strSQL = strSQL + " DescricaoSugerida nvarchar(100) NULL, " + "\r";
				strSQL = strSQL + " SelArtigo bit NULL";


				dynamic[] tempRefParam = new dynamic[]{tmpCabec, tmpDetail, sFileText};
				strSQL = m_objErpBSO.DSO.Plat.Strings.Formata(strSQL, tempRefParam);

				DbCommand TempCommand = null;
				TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
				UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
				TempCommand.CommandText = strSQL;
				UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
				TempCommand.ExecuteNonQuery();

				strSQL = "";

				//Para a Sugerir os artigos por linha recorrendo á referencia de clientes na tabela artigoCLiente

				strSQL = strSQL + "UPDATE temp SET temp.ArtigoSugerido = artCli.Artigo " + "\r";
				strSQL = strSQL + "FROM @2@ AS temp" + "\r";
				strSQL = strSQL + "INNER JOIN ArtigoCliente AS artCli ON temp.productCode = artCli.ReferenciaCli" + "\r";
				strSQL = strSQL + "where temp.ArtigoSugerido IS NULL ";

				//Para a Sugerir os artigos por linha recorrendo á descricao do artigo na tabela artigoCliente

				strSQL = strSQL + "UPDATE temp SET temp.ArtigoSugerido = artCli.Artigo " + "\r";
				strSQL = strSQL + "FROM @2@ AS temp" + "\r";
				strSQL = strSQL + "INNER JOIN ArtigoCliente AS artCli ON temp.productDescription = artCli.DescricaoCli " + "\r";
				strSQL = strSQL + "where temp.ArtigoSugerido IS NULL ";

				//Para a Sugerir os artigos por linha recorrendo código do artigo na tabela Artigo

				strSQL = strSQL + "UPDATE temp SET temp.ArtigoSugerido = art.Artigo " + "\r";
				strSQL = strSQL + "FROM @2@ AS temp" + "\r";
				strSQL = strSQL + "INNER JOIN Artigo AS art ON temp.productCode= art.Artigo " + "\r";
				strSQL = strSQL + "where temp.ArtigoSugerido IS NULL ";

				//Para a Sugerir os artigos por linha recorrendo á descrição do artigo na tabela Artigo

				strSQL = strSQL + "UPDATE temp SET temp.ArtigoSugerido = art.Artigo " + "\r";
				strSQL = strSQL + "FROM @2@ AS temp" + "\r";
				strSQL = strSQL + "INNER JOIN Artigo AS art ON temp.productDescription = art.Descricao " + "\r";
				strSQL = strSQL + "where temp.ArtigoSugerido IS NULL ";

				//Para a Sugerir os artigos por linha para os artigos restantes

				strSQL = strSQL + "UPDATE @2@ SET ArtigoSugerido = ProductCode  where ArtigoSugerido IS NULL ";

				//Para a Sugerir os artigos por linha para os artigos restantes

				strSQL = strSQL + "UPDATE temp SET temp.DescricaoSugerida = art.Descricao " + "\r";
				strSQL = strSQL + "FROM @2@ AS temp" + "\r";
				strSQL = strSQL + "INNER JOIN Artigo AS art ON temp.ArtigoSugerido = art.Artigo " + "\r";
				strSQL = strSQL + "where temp.DescricaoSugerida IS NULL ";

				dynamic[] tempRefParam2 = new dynamic[]{tmpCabec, tmpDetail, sFileText};
				strSQL = m_objErpBSO.DSO.Plat.Strings.Formata(strSQL, tempRefParam2);

				DbCommand TempCommand_2 = null;
				TempCommand_2 = m_objErpBSO.DSO.BDAPL.CreateCommand();
				UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_2);
				TempCommand_2.CommandText = strSQL;
				UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_2);
				TempCommand_2.ExecuteNonQuery();


				return true;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_ImportaAutofacturacaoSAFT", excep.Message);
			}
			return false;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : TrataPayTransacao
		// Description : BID 590093
		// Arguments   : clsDocumentoVenda -->
		// Arguments   : strAvisos         -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public void TrataPayTransacao(VndBE100.VndBEDocumentoVenda clsDocumentoVenda, ref string strAvisos)
		{

			//    If clsDocumentoVenda.Prestacoes.NumItens = 0 And FuncoesBS.Documentos.ValidaRegistaTransacao(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.TipoDoc, clsDocumentoVenda.Serie, clsDocumentoVenda.Entidade, clsDocumentoVenda.TotalDocumento, clsDocumentoVenda.ModoPag, strAvisos) Then
			//
			//            Select Case m_objErpBSO.TransaccoesElectronicas.PayTransacoes.DaEstadoTransacao(clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.TipoDoc, clsDocumentoVenda.Serie, clsDocumentoVenda.Numdoc)
			//                Case 0 'DaEstadoTransacao - PayInexistente
			//                    strIDPayTransacao = m_objErpBSO.TransaccoesElectronicas.PayTransacoes.RegistaTransacao(ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda)
			//                Case 1 'DaEstadoTransacao - PayReferenciaPorGerar
			//                    strIDPayTransacao = m_objErpBSO.TransaccoesElectronicas.PayTransacoes.DaValorAtributo(clsDocumentoVenda.Filial, ConstantesPrimavera100.Modulos.Vendas, clsDocumentoVenda.TipoDoc, clsDocumentoVenda.Serie, clsDocumentoVenda.Numdoc, "Id")
			//                Case Else
			//                    Exit Sub
			//            End Select
			//
			//            If Len(strIDPayTransacao) > 0 Then
			//                m_objErpBSO.TransaccoesElectronicas.PayTransacoes.GeraReferenciaID strIDPayTransacao
			//            End If
			//
			//    End If
		}

		public void TrataPayTransacao(VndBE100.VndBEDocumentoVenda clsDocumentoVenda)
		{
			string tempRefParam505 = "";
			TrataPayTransacao(clsDocumentoVenda, ref tempRefParam505);
		}


		//##SUMMARY - Permite atualizar um bloco de documentos de venda
		//##PARAM DocumentosVenda - Coleção de documentos de venda
		//##PARAM Avisos - Avisos devolvidos na gravação
		public void ActualizaLote(VndBEDocumentosVenda DocumentosVenda, string Avisos)
		{
			VndBE100.VndBEDocumentoVenda objDocumentoVenda = null;
			VndBE100.VndBELinhaDocumentoVenda objLinhaDocumentoVenda = null;
			VndBE100.VndBETabVenda objTabDocumentoVenda = null;
			BasBESerie objSerie = null;
			string strSerieLiquidacao = "";
			int intMultiplicador = 0;
			int lngNumerador = 0;
			string strTipoDoc = "";
			string strSerie = "";
			string strErrosValidacao = "";
			bool blnIniciouTrans = false;
			System.DateTime dtDataDoc = DateTime.FromOADate(0);
			string strHash = "";

			try
			{

				//Validações
				if (DocumentosVenda.NumItens > 50)
				{

					strErrosValidacao = strErrosValidacao + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17677, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}

				if (DocumentosVenda.NumItens == 0)
				{

					strErrosValidacao = strErrosValidacao + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17678, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;

				}

				//Garantir que todos os documentos são da mesma série e do mesmo tipo de documento
				int tempForVar = DocumentosVenda.NumItens;
				for (lngNumerador = 1; lngNumerador <= tempForVar; lngNumerador++)
				{

					if (lngNumerador == 1)
					{

						strSerie = DocumentosVenda.GetEdita(lngNumerador).Serie;
						strTipoDoc = DocumentosVenda.GetEdita(lngNumerador).Tipodoc;

					}

					if (strSerie != DocumentosVenda.GetEdita(lngNumerador).Serie || strTipoDoc != DocumentosVenda.GetEdita(lngNumerador).Tipodoc)
					{

						strErrosValidacao = strErrosValidacao + m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(17679, FuncoesComuns100.InterfaceComunsUS.ModuloGCP) + Environment.NewLine;
						break;

					}


				}

				if (Strings.Len(strErrosValidacao) != 0)
				{

					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.ActualizaLote", strErrosValidacao);

				}

				//Prepara documentos
				foreach (VndBE100.VndBEDocumentoVenda objDocumentoVenda2 in DocumentosVenda)
				{
					objDocumentoVenda = objDocumentoVenda2;


					//Apenas podemos criar documentos novos
					objDocumentoVenda.EmModoEdicao = false;

					PreencheDadosActualiza(ref objDocumentoVenda, ref objTabDocumentoVenda, ref objSerie, ref strSerieLiquidacao, ref intMultiplicador);

					// Se o documento está valido pode seguir
					if (((IVndBS100.IVndBSVendas) this).ValidaActualizacao(objDocumentoVenda, objTabDocumentoVenda, strSerieLiquidacao, Avisos))
					{

						AplicaMultiplicador(intMultiplicador, objDocumentoVenda, true, false);

						//LINHAS
						foreach (VndBE100.VndBELinhaDocumentoVenda objLinhaDocumentoVenda2 in objDocumentoVenda.Linhas)
						{
							objLinhaDocumentoVenda = objLinhaDocumentoVenda2;

							AplicaMultiplicadorLinha(objLinhaDocumentoVenda, intMultiplicador);

							// Copia regime de iva para as linhas
							objLinhaDocumentoVenda.RegimeIva = objDocumentoVenda.RegimeIva;
							CalculaEstadoLinha(objLinhaDocumentoVenda);

							objLinhaDocumentoVenda = null;
						}


					}
					else
					{

						StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.ActualizaLote", Avisos);

					}


					objDocumentoVenda = null;
				}


				blnIniciouTrans = false;
				IniciaTransaccao(ref blnIniciouTrans);

				//Bloqueia a serie
				DbCommand TempCommand = null;
				TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
				UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
				string tempRefParam = "SeriesVendas";
				TempCommand.CommandText = FuncoesComuns100.FuncoesBS.Utils.BloqueiaRegistos(ref tempRefParam, "TipoDoc", strTipoDoc, "Serie", strSerie);
				UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
				TempCommand.ExecuteNonQuery();
				// Numera e assina documentos
				lngNumerador = m_objErpBSO.DSO.Plat.Utils.FLng(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, strTipoDoc, strSerie, "Numerador"));
				strHash = "";

				foreach (VndBE100.VndBEDocumentoVenda objDocumentoVenda3 in DocumentosVenda)
				{
					objDocumentoVenda = objDocumentoVenda3;

					lngNumerador++;
					objDocumentoVenda.NumDoc = lngNumerador;
					dtDataDoc = objDocumentoVenda.DataDoc;
					//Gera assinatura apenas para empresas com sede em Portugal
					CertificacaoSoftware.AssinaDocumento(objDocumentoVenda, objTabDocumentoVenda, objSerie, strHash);
					strHash = objDocumentoVenda.Assinatura;

					objDocumentoVenda = null;
				}


				string tempRefParam2 = "";
				m_objErpBSO.DSO.Vendas.Documentos.ActualizaLote(ref DocumentosVenda, ref tempRefParam2);

				m_objErpBSO.Base.Series.ActualizaNumerador(ConstantesPrimavera100.Modulos.Vendas, strTipoDoc, strSerie, lngNumerador, dtDataDoc);

				m_objErpBSO.TerminaTransaccao();
				blnIniciouTrans = false;

				objDocumentoVenda = null;
				objLinhaDocumentoVenda = null;
				objTabDocumentoVenda = null;
				objSerie = null;
			}
			catch (System.Exception excep)
			{

				if (blnIniciouTrans)
				{
					m_objErpBSO.DesfazTransaccao();
				}
				StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "VndBSVendas.ActualizaLote", excep.Message);
			}

		}

		public void ActualizaLote(ref VndBEDocumentosVenda DocumentosVenda)
		{
			string tempRefParam506 = "";
			ActualizaLote(DocumentosVenda, tempRefParam506);
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSCompras_ValidaCamposSAFT
		// Description : Valida o preenchimento dos campos obrigatórios para o SAF-T
		// Arguments   : Documento -->
		// Arguments   : Erro      -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public bool ValidaCamposSAFT(VndBEDocumentoVenda Documento, string Erro)
		{

			try
			{


				return CertificacaoSoftware.ValidaCamposSAFT(Documento, ref Erro);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_ValidaCamposSAFT", excep.Message);
			}
			return false;
		}

		//---------------------------------------------------------------------------------------
		// Procedure   : PreencheDadosActualiza
		// Description : Prepara o objeto para a atualização
		// Arguments   : DocumentoVenda --> Documento de venda a atualizar
		// Arguments   : TabVenda       --> Definição do documento a atualizar, é passado por referência para o procedimento chamante e deverá ser limpo nesse
		// Arguments   : Serie          --> Definição da série a atualizar, é passado por referência para o procedimento chamante e deverá ser limpo nesse
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void PreencheDadosActualiza(ref VndBE100.VndBEDocumentoVenda DocumentoVenda, ref VndBE100.VndBETabVenda TabVenda, ref BasBESerie Serie, ref string SerieLiq, ref int Multiplicador)
		{

			try
			{

				Contratos.LimpaCacheValores();


				Serie = m_objErpBSO.Base.Series.Edita(ConstantesPrimavera100.Modulos.Vendas, DocumentoVenda.Tipodoc, DocumentoVenda.Serie);

				if (Serie == null)
				{

					string tempRefParam = m_objErpBSO.DSO.Plat.Localizacao.DaResStringApl(9537, FuncoesComuns100.InterfaceComunsUS.ModuloGCP);
					dynamic[] tempRefParam2 = new dynamic[]{DocumentoVenda.Serie};
					StdErros.StdRaiseErro(StdErros.StdErroPrevisto, "_VNDBSVendas.PreparaActualiza", m_objErpBSO.DSO.Plat.Strings.Formata(tempRefParam, tempRefParam2));

				}

				FuncoesComuns100.FuncoesBS.Utils.InitCamposUtil(DocumentoVenda.CamposUtil, DaDefCamposUtil());

				//Edita a configuração do documento de venda
				string tempRefParam3 = DocumentoVenda.Tipodoc;
				TabVenda = m_objErpBSO.Vendas.TabVendas.Edita(tempRefParam3);

				if (TabVenda.LiquidacaoAutomatica)
				{

					SerieLiq = m_objErpBSO.Base.Series.DaSerieDefeito(ConstantesPrimavera100.Modulos.ContasCorrentes, TabVenda.DocumentoLiqAGerar, DocumentoVenda.DataDoc);

				}

				//** Preenche o objecto documento de venda com os dados por defeito
				PreencheDocVenda(ref DocumentoVenda, TabVenda);

				//Se o documento é a pagar os valores são gravados com sinal negativo (PagarReceber = "P" Then Mult = -1 Else Mult = 1)
				//CR.141 - Dá o factor de multiplicação do documento
				Multiplicador = FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.DaFactorNaturezaDoc(ConstantesPrimavera100.Modulos.Vendas, DocumentoVenda.Tipodoc);

				if (!DocumentoVenda.EmModoEdicao)
				{

					if (FuncoesComuns100.FuncoesBS.Utils.LocalizacaoActualPortugal())
					{

						DocumentoVenda.TrataIvaCaixa = (((TabVenda.DeduzLiquidaIVA) ? -1 : 0) & Convert.ToInt32(m_objErpBSO.Contabilidade.ExerciciosCBL.TrataIvaCaixa(DocumentoVenda.DataDoc.Year, DocumentoVenda.DataDoc.Month))) != 0;

						if ((!DocumentoVenda.TrataIvaCaixa) && ((DocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente) || (DocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)))
						{

							DocumentoVenda.TrataIvaCaixa = m_objErpBSO.DSO.Plat.Utils.FBool(FuncoesComuns100.FuncoesBS.Entidades.DaValorAtributoEntidadeVar(DocumentoVenda.TipoEntidade, DocumentoVenda.Entidade, "TrataIvaCaixa")); //PriGlobal: IGNORE

						}

					}
					else if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha)
					{ 

						DocumentoVenda.TrataIvaCaixa = TabVenda.DeduzLiquidaIVA;

						if (DocumentoVenda.TrataIvaCaixa && ((DocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.Cliente) || (DocumentoVenda.TipoEntidade == ConstantesPrimavera100.TiposEntidade.OutroTerceiroDevedor)))
						{

							DocumentoVenda.TrataIvaCaixa = m_objErpBSO.DSO.Plat.Utils.FBool(FuncoesComuns100.FuncoesBS.Entidades.DaValorAtributoEntidadeVar(DocumentoVenda.TipoEntidade, DocumentoVenda.Entidade, "TrataIvaCaixa")); //PriGlobal: IGNORE

						}

					}

				}

				//CS.203_7.50_Alpha7 - Se o documento tiver um Tipo de Lancamento que não faz tratamento fiscal, então o documento fica Isento de IVA
				//Preenche o lancamento
				if (Strings.Len(DocumentoVenda.TipoLancamento) == 0)
				{

					//UPGRADE_WARNING: (1068) m_objErpBSO.Base.Series.DaValorAtributo() of type Variant is being forced to string. More Information: http://www.vbtonet.com/ewis/ewi1068.aspx
					DocumentoVenda.TipoLancamento = ReflectionHelper.GetPrimitiveValue<string>(m_objErpBSO.Base.Series.DaValorAtributo(ConstantesPrimavera100.Modulos.Vendas, DocumentoVenda.Tipodoc, DocumentoVenda.Serie, "TipoLancamento"));

				}

				//Se o tipo de lancamento não faz tratamento fiscal, então coloca o regime de Iva a isento para não tratar o IVA
				if (~Convert.ToInt32(m_objErpBSO.Contabilidade.TiposLancamento.TrataFiscal(DocumentoVenda.TipoLancamento)) != 0)
				{

					DocumentoVenda.RegimeIva = ((int) BasBETipos.LOGEspacoFiscalDoc.MercadoNacionalIsentoIva).ToString();

				}
				//END CS.203_7.50_Alpha7

				//Calcular valores totais do documento de venda
				CalculaTotaisDocumento(ref DocumentoVenda);

				if (!DocumentoVenda.EmModoEdicao)
				{

					DocumentoVenda.Tipodoc = DocumentoVenda.Tipodoc.ToUpper();
					if (TabVenda.TipoDocumento != ((int) BasBETipos.LOGTipoDocumento.LOGDocCotacao))
					{

						DocumentoVenda.Estado = "P";

					}
					else
					{

						if (Strings.Len(DocumentoVenda.Estado) == 0)
						{

							DocumentoVenda.Estado = "G";

						}

					}

				}

				//Limpa os campos da certificação
				CertificacaoSoftware.LimpaCamposCertificacao(DocumentoVenda, TabVenda, Serie);

				if (Strings.Len(DocumentoVenda.EntidadeDescarga) == 0 && Strings.Len(DocumentoVenda.EntidadeEntrega) > 0)
				{

					DocumentoVenda.EntidadeDescarga = DocumentoVenda.EntidadeEntrega;

				}
				else if (Strings.Len(DocumentoVenda.EntidadeEntrega) == 0 && Strings.Len(DocumentoVenda.EntidadeDescarga) > 0)
				{ 

					DocumentoVenda.EntidadeEntrega = DocumentoVenda.EntidadeDescarga;

				}

				//Faz a preparação para a gravação, carregando dados já previamente gravados
				CertificacaoSoftware.PreparaGravacaoDocumento(DocumentoVenda, TabVenda, Serie);
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_PreparaActualiza_ImportaAutofacturacaoSAFT", excep.Message);
			}


		}

		private void AplicaMultiplicadorLinha(VndBE100.VndBELinhaDocumentoVenda LinhaVenda, int Multiplicador)
		{

			try
			{

				// Inicializa o ID da linha
				if (Strings.Len(LinhaVenda.IdLinha) == 0)
				{

					bool tempRefParam = true;
					LinhaVenda.IdLinha = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempRefParam);

				}

				//CR.141 - Garante que o PrecUnit vai para a BD com o mesmo sinal que a quantidade
				if (LinhaVenda.Quantidade < 0)
				{

					LinhaVenda.PrecUnit = (-1 * Math.Abs(LinhaVenda.PrecUnit));

				}

				LinhaVenda.Quantidade *= Multiplicador;
				LinhaVenda.PrecUnit *= Multiplicador;
				LinhaVenda.TotalIliquido *= Multiplicador;
				LinhaVenda.TotalDA *= Multiplicador;
				LinhaVenda.TotalDC *= Multiplicador;
				LinhaVenda.TotalDF *= Multiplicador;
				LinhaVenda.DescontoComercial *= Multiplicador;
				LinhaVenda.PrecoLiquido *= Multiplicador;
				LinhaVenda.TotalIva *= Multiplicador;

				if (m_objErpBSO.Contexto.LocalizacaoSede == ErpBS100.StdBEContexto.EnumLocalizacaoSede.lsEspanha)
				{

					LinhaVenda.TotalRecargo *= Multiplicador;

				}

				LinhaVenda.TotalEcotaxa *= Multiplicador;
				LinhaVenda.IvaNaoDedutivel *= Multiplicador;

				//CS.242_7.50_Alfa8
				LinhaVenda.TotalIEC *= Multiplicador;

				//CS.3483 - Regimes especiais de IVA e IPC
				LinhaVenda.BaseCalculoIncidencia *= Multiplicador;
				LinhaVenda.BaseIncidencia *= Multiplicador;
				LinhaVenda.DadosImpostoSelo.IncidenciaIS *= Multiplicador;
				LinhaVenda.DadosImpostoSelo.ValorIS *= Multiplicador;

				//User Story 4769:Como utilizador quero ter uma coluna com os descontos rateados das linhas especiais de desconto
				LinhaVenda.ValorLiquidoDesconto *= Multiplicador;
				LinhaVenda.IvaValorDesconto *= Multiplicador;
			}
			catch (System.Exception excep)
			{
				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_AplicaMultiplicadorLinha", excep.Message);
			}

		}

		private double DaTotalReservado(dynamic Reservas, string Artigo = "", string Armazem = "", string Localizacao = "", string Lote = "", string EstadoOrigem = "")
		{
			double Total = 0;

			try
			{

				Total = 0;

				foreach (dynamic Reserva in Reservas)
				{

					if ((Strings.Len(Artigo) == 0 || Reserva.Artigo == Artigo) && (Strings.Len(Armazem) == 0 || Reserva.Armazem == Armazem) && (Strings.Len(Localizacao) == 0 || Reserva.Localizacao == Localizacao) && (Strings.Len(Lote) == 0 || Reserva.Lote == Lote) && (Strings.Len(EstadoOrigem) == 0 || Reserva.EstadoOrigem == EstadoOrigem))
					{

						Total += Reserva.Quantidade;

					}

					//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
					//Reserva = null;

				}


				return Total;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_DaTotalReservado", excep.Message);
			}
			return 0;
		}

		private void ActualizaReservas(VndBE100.VndBEDocumentoVenda Documento)
		{

			dynamic Reserva = null;
			dynamic TodasReservas = null;
			double dblQuantReservada = 0;
			StdBE100.StdBECampos objCampos = null;
			double dblQtdRes = 0;
			double dblQtdPendRes = 0;
			string strSQL = "";

			try
			{

				//TodasReservas = new dynamic();

				foreach (VndBE100.VndBELinhaDocumentoVenda Linha in Documento.Linhas)
				{

					Reserva = Linha.ReservaStock;

					if (Reserva != null)
					{

						foreach (dynamic LinhaReserva in Reserva.Linhas)
						{

							if (!LinhaReserva.EmModoEdicao)
							{

								LinhaReserva.ReservadoPor = InvBE100.InvBETipos.EnumReservadoPor.Destino;
								LinhaReserva.IdChaveDestino = Linha.IdLinha;
								LinhaReserva.QuantidadePendente = LinhaReserva.Quantidade;

							}
							else
							{

								objCampos = (StdBE100.StdBECampos) m_objErpBSO.Inventario.Reservas.DaValorAtributos(LinhaReserva.ID, "Quantidade", "QuantidadePendente");
								if (objCampos != null)
								{

									string tempRefParam = "Quantidade";
									dblQtdRes = m_objErpBSO.DSO.Plat.Utils.FDbl(objCampos.GetItem(ref tempRefParam));
									string tempRefParam2 = "QuantidadePendente";
									dblQtdPendRes = m_objErpBSO.DSO.Plat.Utils.FDbl(objCampos.GetItem(ref tempRefParam2));
									objCampos = null;

								}



							}

							//Se estamos a reduzir a qtd e temos quantidade já satisfeita, a quantidade da reserva terá que ter essa quantidade em conta
							if (dblQtdRes > dblQtdPendRes && dblQtdRes != LinhaReserva.Quantidade)
							{

								LinhaReserva.Quantidade += (dblQtdRes - dblQtdPendRes);
								LinhaReserva.QuantidadePendente -= (dblQtdRes - LinhaReserva.Quantidade);

							}

							if (dblQtdRes == dblQtdPendRes)
							{

								LinhaReserva.QuantidadePendente = LinhaReserva.Quantidade;

							}

							if (LinhaReserva.QuantidadePendente > LinhaReserva.Quantidade)
							{

								LinhaReserva.QuantidadePendente = LinhaReserva.Quantidade;

							}
							LinhaReserva.DescricaoDestino = Documento.Documento;
							LinhaReserva.TipoDocDestino = Documento.Tipodoc;
							dblQuantReservada += LinhaReserva.Quantidade;

							//Marcar o documento origem como desatualizado
							if (Strings.Len(LinhaReserva.IdChaveOrigem) > 0)
							{

								if (Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaModuloTipoOrigem(LinhaReserva.IdTipoOrigemOrigem)) == ConstantesPrimavera100.Modulos.Compras)
								{

									strSQL = "UPDATE C SET C.Desatualizado = 1" + Environment.NewLine;
									strSQL = strSQL + "FROM CabecCompras C" + Environment.NewLine;
									strSQL = strSQL + "INNER JOIN LinhasCompras L ON L.IdCabecCompras = C.Id" + Environment.NewLine;
									strSQL = strSQL + "WHERE L.Id = '@1@'";
									dynamic[] tempRefParam3 = new dynamic[]{LinhaReserva.IdChaveOrigem};
									strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);
									DbCommand TempCommand = null;
									TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
									UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
									TempCommand.CommandText = strSQL;
									UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
									TempCommand.ExecuteNonQuery();

								}

							}

							TodasReservas.Linhas.Insere(LinhaReserva);

							//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
							//LinhaReserva = null;
						}

						m_objErpBSO.Inventario.Reservas.Actualiza(Reserva);
						Linha.QuantReservada = dblQuantReservada;

					}

					//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
					//Linha = null;
					Reserva = null;
				}

				m_objErpBSO.Compras.Documentos.ActualizaQtdReservadaOrigem(TodasReservas);

				Reserva = null;
				TodasReservas = null;
			}
			catch (System.Exception excep)
			{



				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_ActualizaReservas", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : RemoveReservasCanceladas
		// Description :
		// Arguments   : Documento -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void RemoveReservasCanceladas(VndBE100.VndBEDocumentoVenda Documento)
		{
			string strIdTipoOrigemDestino = "";
			string strIdReserva = "";
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			double dblQtdReserva = 0;
			dynamic objOrigens = null;
			StdBE100.StdBELista objListaAnula = null;
			bool blnRemoveu = false;

			try
			{

				strIdTipoOrigemDestino = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));

				int tempForVar = Documento.Linhas.Removidas.NumItens;
				for (int lngIndex = 1; lngIndex <= tempForVar; lngIndex++)
				{

					m_objErpBSO.Inventario.Reservas.RemoveDestino(strIdTipoOrigemDestino, Documento.Linhas.Removidas.GetEdita(lngIndex), true);

				}

				//Percorrem-se todas as linhas e remove-se as reservas antigas
				foreach (VndBE100.VndBELinhaDocumentoVenda ObjLinha in Documento.Linhas)
				{

					if (ObjLinha.ReservaStock != null)
					{

						int tempForVar2 = ObjLinha.ReservaStock.Linhas.Removidas.NumItens;
						for (int lngIndex = 1; lngIndex <= tempForVar2; lngIndex++)
						{

							strIdReserva = ObjLinha.ReservaStock.Linhas.Removidas.GetEdita(lngIndex);

							if (Strings.Len(strIdReserva) > 0)
							{

								//Apenas se a linha já não existe
								if (!ObjLinha.ReservaStock.Linhas.GetExisteId(strIdReserva))
								{

									dblQtdReserva = m_objErpBSO.DSO.Plat.Utils.FDbl(m_objErpBSO.Inventario.Reservas.DaValorAtributo(strIdReserva, "Quantidade"));

									strSQL = ";WITH CTE_Reservas(Id, IdReservaOriginal, Quantidade)" + Environment.NewLine;
									strSQL = strSQL + "AS" + Environment.NewLine;
									strSQL = strSQL + "(" + Environment.NewLine;
									strSQL = strSQL + " SELECT Id, IdReservaOriginal, Quantidade FROM INV_Reservas (NOLOCK) WHERE Id = '@1@'" + Environment.NewLine;
									strSQL = strSQL + " UNION ALL" + Environment.NewLine;
									strSQL = strSQL + " SELECT R.Id, R.IdReservaOriginal, CAST(R.Quantidade - (R.QuantidadePendente + CTE.Quantidade) AS DECIMAL(28,10))" + Environment.NewLine;
									strSQL = strSQL + "   FROM INV_Reservas (NOLOCK) R" + Environment.NewLine;
									strSQL = strSQL + " INNER JOIN CTE_Reservas CTE ON CTE.IdReservaOriginal = R.Id" + Environment.NewLine;
									strSQL = strSQL + ")" + Environment.NewLine;
									strSQL = strSQL + "SELECT Id, Quantidade FROM CTE_Reservas";
									dynamic[] tempRefParam = new dynamic[]{strIdReserva};
									strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);
									objLista = m_objErpBSO.Consulta(strSQL);

									dynamic tempRefParam2 = objLista;
									if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam2))
									{
										objLista = (StdBE100.StdBELista) tempRefParam2;

										blnRemoveu = false;
										while (!objLista.NoFim())
										{

											MarcaDocumentoDesatualizado("" + objLista.Valor("Id"));

											if (m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("Quantidade")) == 0 || String.Compare(objLista.Valor("Id"), strIdReserva, true) == 0)
											{

												m_objErpBSO.Inventario.Reservas.RemoveID("" + objLista.Valor("Id"));
												blnRemoveu = true;

											}
											else
											{

												//Actualiza a reserva
												strSQL = "UPDATE INV_Reservas SET Quantidade = Quantidade - @1@, QuantidadePendente = QuantidadePendente - @1@ WHERE Id = '@2@'";
												dynamic[] tempRefParam3 = new dynamic[]{dblQtdReserva, "" + objLista.Valor("Id")};
												strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);
												DbCommand TempCommand = null;
												TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
												UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
												TempCommand.CommandText = strSQL;
												UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
												TempCommand.ExecuteNonQuery();

												if (!blnRemoveu)
												{

													//objOrigens = new dynamic();
													m_objErpBSO.Inventario.Reservas.PreencheOrigensReducaoReserva(objOrigens, "" + objLista.Valor("Id"), "" + objLista.Valor("Id"), dblQtdReserva);
													m_objErpBSO.Inventario.Documentos.Actualiza(objOrigens);
													objOrigens = null;

												}

												strSQL = "UPDATE INV_Reservas SET Fechada = 1 WHERE Id = '@1@' AND QuantidadePendente = 0";
												dynamic[] tempRefParam4 = new dynamic[]{"" + objLista.Valor("Id")};
												strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam4);
												DbCommand TempCommand_2 = null;
												TempCommand_2 = m_objErpBSO.DSO.BDAPL.CreateCommand();
												UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_2);
												TempCommand_2.CommandText = strSQL;
												UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_2);
												TempCommand_2.ExecuteNonQuery();

												//Verificar se a reserva chegou a zero
												strSQL = "SELECT Id FROM INV_Reservas (NOLOCK) WHERE Id = '@1@' AND Quantidade = 0";
												dynamic[] tempRefParam5 = new dynamic[]{"" + objLista.Valor("Id")};
												strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam5);
												objListaAnula = m_objErpBSO.Consulta(strSQL);
												dynamic tempRefParam6 = objListaAnula;
												if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam6))
												{
													objListaAnula = (StdBE100.StdBELista) tempRefParam6;

													m_objErpBSO.Inventario.Reservas.RemoveID("" + objLista.Valor("Id"));

												}
												else
												{
													objListaAnula = (StdBE100.StdBELista) tempRefParam6;
												}

											}

											objLista.Seguinte();

										}

									}
									else
									{
										objLista = (StdBE100.StdBELista) tempRefParam2;
									}

								}

							}

						}

					}

					//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
					//ObjLinha = null;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_RemoveReservasCanceladas", excep.Message);
			}

		}


		//---------------------------------------------------------------------------------------
		// Procedure   : MarcaDocumentoDesatualizado
		// Description : Marca o documento de compra referenciado na reserva alterada/removida como desatualizada
		// Arguments   : IdReserva -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void MarcaDocumentoDesatualizado(string IdReserva)
		{
			dynamic objReserva = null;
			dynamic objLinhaReserva = null;
			string strSQL = "";

			try
			{

				objReserva = (dynamic) m_objErpBSO.Inventario.Reservas.EditaId(IdReserva, true);

				foreach (dynamic objLinhaReserva2 in objReserva.Linhas)
				{
					objLinhaReserva = objLinhaReserva2;

					//Marcar o documento origem como desatualizado, apenas se não é de reserva automática
					if (Strings.Len(objLinhaReserva.IdChaveOrigem) > 0)
					{

						if (Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaModuloTipoOrigem(objLinhaReserva.IdTipoOrigemOrigem)) == ConstantesPrimavera100.Modulos.Compras)
						{

							strSQL = "UPDATE C SET C.Desatualizado = 1" + Environment.NewLine;
							strSQL = strSQL + "FROM CabecCompras C" + Environment.NewLine;
							strSQL = strSQL + "INNER JOIN LinhasCompras L ON L.IdCabecCompras = C.Id" + Environment.NewLine;
							strSQL = strSQL + "WHERE L.Id = '@1@'";
							dynamic[] tempRefParam = new dynamic[]{objLinhaReserva.IdChaveOrigem};
							strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);
							DbCommand TempCommand = null;
							TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
							UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
							TempCommand.CommandText = strSQL;
							UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
							TempCommand.ExecuteNonQuery();

						}

					}

					objLinhaReserva = null;
				}


				objReserva = null;
				objLinhaReserva = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_MarcaDocumentoDesatualizado", excep.Message);
			}

		}

		private double DaStockReservadoDoc(string IdTipoOrigemDestino, string IdDocReserva, string Artigo, string Armazem, string Localizacao, string Lote, string EstadoOrigem)
		{
			double result = 0;
			string strSQL = "";
			StdBE100.StdBELista objLista = null;

			try
			{

				if (Strings.Len(IdDocReserva) == 0)
				{
					return 0;
				}

				strSQL = "SELECT ISNULL(SUM(R.Quantidade),0) Quant " + 
				         "FROM INV_Reservas R " + 
				         "INNER JOIN LinhasDoc L ON  L.Id = R.IdChaveDestino  " + 
				         "INNER JOIN CabecDoc C ON C.Id = L.IdCabecDoc " + 
				         "WHERE R.IdTipoOrigemDestino = '@1@' AND C.Id = '@2@' " + 
				         "AND R.Artigo = '@3@' AND R.Armazem ='@4@' AND R.Localizacao = '@5@' " + 
				         "AND R.EstadoOrigem = '@6@' ";


				if (Strings.Len(Lote) > 0)
				{
					string tempRefParam = "AND R.Lote = '@1@' ";
					dynamic[] tempRefParam2 = new dynamic[]{Lote};
					strSQL = strSQL + m_objErpBSO.DSO.Plat.Sql.FormatSQL(tempRefParam, tempRefParam2);
				}

				dynamic[] tempRefParam3 = new dynamic[]{IdTipoOrigemDestino, IdDocReserva, Artigo, Armazem, Localizacao, EstadoOrigem};
				strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);

				objLista = m_objErpBSO.Consulta(strSQL);

				result = m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("Quant"));

				objLista = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VNDBSVendas.DaStockReservadoDoc", excep.Message);
			}

			return result;
		}
		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_SugereReservasAutomaticas
		// Description : Sugere uma lista de reservas tendo em conta as disponibilidades de stock
		// Arguments   : clsDocumentoVenda --
		// Arguments   : Artigo --> Artigo a reservar
		// Arguments   : Quantidade --> Quantidade a reservar
		// Arguments   : IdTipoOrigemDestino -> Identificador da Origem (STK) associada ao Documento de destino da Reserva
		// Arguments   : IdChaveDestino --> Identificador da linha de destino da reserva
		// Arguments   : DescricaoDestino --> Descriçao do destinatario da reserva
		// Arguments   : EstadoDestino --> Estado de Reserva (Destino)
		// Arguments   : Colecção de outras reservas pendentes que ainda não estão materializadas mas devem ser contabilizadas
		// Arguments   : Armazem --> Armazem de origem da reserva
		// Arguments   : Localizacao --> Localizacao da origem da reserva
		// Arguments   : Lote --> Lote do Artigo a reservar
		// Arguments   : IdDocReserva --> Identificador do documento de origem cujas reservas devem excluidas
		// Returns     : Colecção de Reservas sugeridas
		//---------------------------------------------------------------------------------------
		public dynamic SugereReservasAutomaticas(string Artigo, double Quantidade, string IdTipoOrigemDestino, string IdChaveDestino, string DescricaoDestino, string EstadoDestino,  dynamic ReservasPendentes, string Armazem, string Localizacao, string Lote,  string IdDocReserva)
		{


			dynamic result = null;
			StdBE100.StdBELista lstStockDisponivel = null;
			double dblStockDisponivel = 0;
			double dblStockPendReserva = 0;
			double dblStockReservar = 0;
			double dblStockReservavel = 0;
			double dblStockNecessario = 0;
			double dblQuantReservada = 0;
			dynamic objReserva = null;
			dynamic objLinhaReserva = null;

			try
			{

				//objReserva = new dynamic();

				//Apenas se a linha tem armazém e localização
				if (Strings.Len(Armazem) > 0)
				{

					lstStockDisponivel = (StdBE100.StdBELista) m_objErpBSO.Inventario.Stocks.ListaStockLocalizacaoLote2(Artigo, null, Armazem, Localizacao, Lote, InvBE100.InvBETipos.FlagFiltroEstado.Sim);

					dblQuantReservada = 0;

					while (!lstStockDisponivel.NoFim() && dblQuantReservada < Quantidade)
					{

						dblStockDisponivel = Double.Parse(lstStockDisponivel.Valor("Stock")) + DaStockReservadoDoc(IdTipoOrigemDestino, IdDocReserva, Artigo, Armazem, Localizacao, Lote, lstStockDisponivel.Valor("EstadoStock"));
						dblStockPendReserva = DaTotalReservado(ReservasPendentes, Artigo, Armazem, Localizacao, Lote, lstStockDisponivel.Valor("EstadoStock"));
						dblStockReservavel = dblStockDisponivel - dblStockPendReserva;
						dblStockNecessario = Quantidade - dblQuantReservada;
						dblStockReservar = FuncoesGlobais.Minimum(dblStockReservavel, dblStockNecessario);

						if (dblStockReservar > 0)
						{

							//objLinhaReserva = new dynamic();
							objReserva.Linhas.Insere(objLinhaReserva);

							objLinhaReserva.EmModoEdicao = false;

							bool tempParam = true;
							objLinhaReserva.ID = m_objErpBSO.DSO.Plat.FuncoesGlobais.CriaGuid(ref tempParam);
							objLinhaReserva.EstadoOrigem = lstStockDisponivel.Valor("EstadoStock");
							objLinhaReserva.IdTipoOrigemDestino = IdTipoOrigemDestino;
							objLinhaReserva.IdChaveDestino = IdChaveDestino;
							objLinhaReserva.DescricaoDestino = DescricaoDestino;
							objLinhaReserva.EstadoDestino = EstadoDestino;

							objLinhaReserva.Quantidade = dblStockReservar;
							objLinhaReserva.QuantidadePendente = dblStockReservar;

							objLinhaReserva.Artigo = Artigo;
							objLinhaReserva.Armazem = Armazem;
							objLinhaReserva.Localizacao = Localizacao;
							objLinhaReserva.Lote = Lote;

							objLinhaReserva.ReservadoPor = InvBE100.InvBETipos.EnumReservadoPor.Destino;

							dblQuantReservada += dblStockReservar;

							ReservasPendentes.Insere(objLinhaReserva);

							objLinhaReserva = null;

						}

						lstStockDisponivel.Seguinte();

					}

				}

				result = objReserva;

				lstStockDisponivel = null;
				objReserva = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_IVndBSVendas_SugereReservasAutomaticas", excep.Message);
			}


			return result;
		}

		public dynamic SugereReservasAutomaticas(string Artigo, double Quantidade, string IdTipoOrigemDestino, string IdChaveDestino, string DescricaoDestino, string EstadoDestino,  dynamic ReservasPendentes, string Armazem, string Localizacao, string Lote)
		{
			string tempParam507 = "";
			return SugereReservasAutomaticas(Artigo, Quantidade, IdTipoOrigemDestino, IdChaveDestino, DescricaoDestino, EstadoDestino,  ReservasPendentes, Armazem, Localizacao, Lote,  tempParam507);
		}

		public dynamic SugereReservasAutomaticas(string Artigo, double Quantidade, string IdTipoOrigemDestino, string IdChaveDestino, string DescricaoDestino, string EstadoDestino,  dynamic ReservasPendentes, string Armazem, string Localizacao)
		{
			string tempParam508 = "";
			return SugereReservasAutomaticas(Artigo, Quantidade, IdTipoOrigemDestino, IdChaveDestino, DescricaoDestino, EstadoDestino,  ReservasPendentes, Armazem, Localizacao, "",  tempParam508);
		}

		public dynamic SugereReservasAutomaticas(string Artigo, double Quantidade, string IdTipoOrigemDestino, string IdChaveDestino, string DescricaoDestino, string EstadoDestino,  dynamic ReservasPendentes, string Armazem)
		{
			string tempParam509 = "";
			return SugereReservasAutomaticas(Artigo, Quantidade, IdTipoOrigemDestino, IdChaveDestino, DescricaoDestino, EstadoDestino,  ReservasPendentes, Armazem, "", "",  tempParam509);
		}

		public dynamic SugereReservasAutomaticas(string Artigo, double Quantidade, string IdTipoOrigemDestino, string IdChaveDestino, string DescricaoDestino, string EstadoDestino,  dynamic ReservasPendentes)
		{
			string tempParam510 = "";
			return SugereReservasAutomaticas(Artigo, Quantidade, IdTipoOrigemDestino, IdChaveDestino, DescricaoDestino, EstadoDestino,  ReservasPendentes, "", "", "",  tempParam510);
		}


		private void PreencheReservasLinhas(VndBE100.VndBEDocumentoVenda Doc)
		{
			VndBE100.VndBELinhaDocumentoVenda Linha = null;
			string IdTipoOrigem = "";

			try
			{

				IdTipoOrigem = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));

				foreach (VndBE100.VndBELinhaDocumentoVenda Linha2 in Doc.Linhas)
				{
					Linha = Linha2;

					Linha.ReservaStock = (dynamic) m_objErpBSO.Inventario.Reservas.EditaDestino(IdTipoOrigem, Linha.IdLinha);

					Linha = null;
				}


				Linha = null;
			}
			catch (System.Exception excep)
			{


				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_PreencheReservasLinhas", excep.Message);
			}

		}

		//##SUMMARY Actualiza quantidades reservadas (LinhasStatus) para os destinatarios
		//##PARAM Reservas Lista de Reservas a considerar
		public void ActualizaQtdReservadaDestino(dynamic Reservas)
		{
			string strIdTipoOrigem = "";
			string strSQL = "";

			try
			{

				strIdTipoOrigem = Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Vendas, ConstantesPrimavera100.Modulos.Vendas));

				foreach (dynamic objLinhaReserva in Reservas.Linhas)
				{

					if (objLinhaReserva.IdTipoOrigemDestino == strIdTipoOrigem && Strings.Len(objLinhaReserva.IdChaveDestino) > 0)
					{

						strSQL = "";
						strSQL = strSQL + "UPDATE LinhasDocStatus" + Environment.NewLine;
						strSQL = strSQL + "    SET QuantReserv =" + Environment.NewLine;
						strSQL = strSQL + "        (" + Environment.NewLine;
						strSQL = strSQL + "             SELECT SUM(QuantidadePendente)" + Environment.NewLine;
						strSQL = strSQL + "             FROM   INV_Reservas" + Environment.NewLine;
						strSQL = strSQL + "             WHERE  IdTipoOrigemDestino = '@1@' AND IdChaveDestino = '@2@' AND Fechada = 0" + Environment.NewLine;
						strSQL = strSQL + "        ) " + Environment.NewLine;
						strSQL = strSQL + "WHERE IdLinhasDoc =  '@2@'" + Environment.NewLine;

						dynamic[] tempRefParam = new dynamic[]{strIdTipoOrigem, objLinhaReserva.IdChaveDestino};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

						DbCommand TempCommand = null;
						TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
						UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
						TempCommand.CommandText = strSQL;
						UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
						TempCommand.ExecuteNonQuery();

					}

					//UPGRADE_NOTE: (2041) The following line was commented. More Information: http://www.vbtonet.com/ewis/ewi2041.aspx
					//objLinhaReserva = null;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_ActualizaQtdReservadaDestino", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : IVndBSVendas_LstEncomendasAReservar
		// Description :
		// Arguments   : Artigo           -->
		// Arguments   : IncluiEncomendas -->
		// Arguments   : Armazem          -->
		// Arguments   : Localizacao      -->
		// Arguments   : Lote             -->
		// Arguments   : CampoSelect      -->
		// Arguments   : DataInicial      -->
		// Arguments   : DataFinal        -->
		// Arguments   : RestricoesSQL    -->
		// Arguments   : TabelaTemp       -->
		//Arguments    : IdDocReserva     -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial, System.DateTime DataFinal, string RestricoesSQL, string TabelaTemp, string IdDocReserva, string IdChaveOriginal)
		{

			string tempRefParam = ConstantesPrimavera100.Modulos.Vendas;
			return FuncoesComuns100.FuncoesBS.Documentos.LstEncomendasParaReserva(ref tempRefParam, ref Artigo, IncluiEncomendas, false, ref CampoSelect, ref DataInicial, ref DataFinal, false, RestricoesSQL, ref TabelaTemp, ref IdDocReserva, ref IdChaveOriginal);

		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial, System.DateTime DataFinal, string RestricoesSQL, string TabelaTemp, string IdDocReserva)
		{
			string tempRefParam511 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, DataInicial, DataFinal, RestricoesSQL, TabelaTemp, IdDocReserva, tempRefParam511);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial, System.DateTime DataFinal, string RestricoesSQL, string TabelaTemp)
		{
			string tempRefParam512 = "";
			string tempRefParam513 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, DataInicial, DataFinal, RestricoesSQL, TabelaTemp, tempRefParam512, tempRefParam513);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial, System.DateTime DataFinal, string RestricoesSQL)
		{
			string tempRefParam514 = "";
			string tempRefParam515 = "";
			string tempRefParam516 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, DataInicial, DataFinal, RestricoesSQL, tempRefParam514, tempRefParam515, tempRefParam516);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial, System.DateTime DataFinal)
		{
			string tempRefParam517 = "";
			string tempRefParam518 = "";
			string tempRefParam519 = "";
			string tempRefParam520 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, DataInicial, DataFinal, tempRefParam517, tempRefParam518, tempRefParam519, tempRefParam520);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect, System.DateTime DataInicial)
		{
			System.DateTime tempRefParam521 = DateTime.FromOADate(0);
			string tempRefParam522 = "";
			string tempRefParam523 = "";
			string tempRefParam524 = "";
			string tempRefParam525 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, DataInicial, tempRefParam521, tempRefParam522, tempRefParam523, tempRefParam524, tempRefParam525);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote, string CampoSelect)
		{
			System.DateTime tempRefParam526 = DateTime.FromOADate(0);
			System.DateTime tempRefParam527 = DateTime.FromOADate(0);
			string tempRefParam528 = "";
			string tempRefParam529 = "";
			string tempRefParam530 = "";
			string tempRefParam531 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, CampoSelect, tempRefParam526, tempRefParam527, tempRefParam528, tempRefParam529, tempRefParam530, tempRefParam531);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao, string Lote)
		{
			string tempRefParam532 = "*";
			System.DateTime tempRefParam533 = DateTime.FromOADate(0);
			System.DateTime tempRefParam534 = DateTime.FromOADate(0);
			string tempRefParam535 = "";
			string tempRefParam536 = "";
			string tempRefParam537 = "";
			string tempRefParam538 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, Lote, tempRefParam532, tempRefParam533, tempRefParam534, tempRefParam535, tempRefParam536, tempRefParam537, tempRefParam538);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem, string Localizacao)
		{
			string tempRefParam539 = "*";
			System.DateTime tempRefParam540 = DateTime.FromOADate(0);
			System.DateTime tempRefParam541 = DateTime.FromOADate(0);
			string tempRefParam542 = "";
			string tempRefParam543 = "";
			string tempRefParam544 = "";
			string tempRefParam545 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, Localizacao, "", tempRefParam539, tempRefParam540, tempRefParam541, tempRefParam542, tempRefParam543, tempRefParam544, tempRefParam545);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas, string Armazem)
		{
			string tempRefParam546 = "*";
			System.DateTime tempRefParam547 = DateTime.FromOADate(0);
			System.DateTime tempRefParam548 = DateTime.FromOADate(0);
			string tempRefParam549 = "";
			string tempRefParam550 = "";
			string tempRefParam551 = "";
			string tempRefParam552 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, Armazem, "", "", tempRefParam546, tempRefParam547, tempRefParam548, tempRefParam549, tempRefParam550, tempRefParam551, tempRefParam552);
		}

		public StdBE100.StdBELista LstEncomendasAReservar(string Artigo, bool IncluiEncomendas)
		{
			string tempRefParam553 = "*";
			System.DateTime tempRefParam554 = DateTime.FromOADate(0);
			System.DateTime tempRefParam555 = DateTime.FromOADate(0);
			string tempRefParam556 = "";
			string tempRefParam557 = "";
			string tempRefParam558 = "";
			string tempRefParam559 = "";
			return LstEncomendasAReservar(Artigo, IncluiEncomendas, "", "", "", tempRefParam553, tempRefParam554, tempRefParam555, tempRefParam556, tempRefParam557, tempRefParam558, tempRefParam559);
		}


		//---------------------------------------------------------------------------------------
		// Procedure   : PreencheNumerosSerieOrigemReserva
		// Description : Lê os números de série do documento que deu entrada do stock num documento convertido com reservas
		// Arguments   : LinhaDoc -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void PreencheNumerosSerieOrigemReserva(VndBE100.VndBELinhaDocumentoVenda LinhaDoc)
		{
			StdBE100.StdBECampos objCampos = null;
			string strIdLinhaOrig = "";
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			BasBENumeroSerie objNumSerie = null;

			try
			{

				//Apenas se temos id de reserva e o artigo trata números de série
				if (Strings.Len(LinhaDoc.INV_IDReserva) == 0)
				{

					return;

				}

				if (!m_objErpBSO.DSO.Plat.Utils.FBool(m_objErpBSO.Base.Artigos.DaValorAtributo(LinhaDoc.Artigo, "TratamentoSeries")))
				{

					return;

				}

				objCampos = (StdBE100.StdBECampos) m_objErpBSO.Inventario.Reservas.DaValorAtributos(LinhaDoc.INV_IDReserva, "IdTipoOrigemOrigem", "IdChaveOrigem");
				if (objCampos != null)
				{

					string tempRefParam = "IdTipoOrigemOrigem";
					if (String.Compare(m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam)), Convert.ToString(m_objErpBSO.Inventario.TiposOrigem.DaIDTipoOrigem(ConstantesPrimavera100.AbreviaturasApl.Compras, ConstantesPrimavera100.Modulos.Compras)), true) == 0)
					{

						string tempRefParam2 = "IdChaveOrigem";
						strIdLinhaOrig = m_objErpBSO.DSO.Plat.Utils.FStr(objCampos.GetItem(ref tempRefParam2));
						strSQL = "SELECT NumSerie, IdNumeroSerie FROM LinhasNumSerie (NOLOCK) WHERE IdLinhas ='@1@'";
						dynamic[] tempRefParam3 = new dynamic[]{strIdLinhaOrig};
						strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);
						objLista = m_objErpBSO.Consulta(strSQL);
						dynamic tempRefParam4 = objLista;
						if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam4))
						{
							objLista = (StdBE100.StdBELista) tempRefParam4;

							//Caso existam mais números de série no documento de venda origem do que no documento de compra,
							//Remove os números que existem no documento de compra para os inserir
							if (LinhaDoc.Quantidade > objLista.NumLinhas())
							{

								int tempForVar = objLista.NumLinhas();
								for (int lngIndice = 1; lngIndice <= tempForVar; lngIndice++)
								{

									LinhaDoc.NumerosSerie.Remove(m_objErpBSO.DSO.Plat.Utils.FInt(lngIndice));

								}

							}
							else
							{

								LinhaDoc.NumerosSerie.RemoveTodos();

							}

							while (!objLista.NoFim())
							{

								objNumSerie = new BasBENumeroSerie();
								objNumSerie.NumeroSerie = m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("NumSerie"));
								objNumSerie.IdNumeroSerie = m_objErpBSO.DSO.Plat.Utils.FStr(objLista.Valor("IdNumeroSerie"));
								objNumSerie.Modulo = ConstantesPrimavera100.Modulos.Vendas;

								LinhaDoc.NumerosSerie.Insere(objNumSerie);
								objNumSerie = null;
								objLista.Seguinte();

							}

						}
						else
						{
							objLista = (StdBE100.StdBELista) tempRefParam4;
						}

						objLista = null;


					}

					objCampos = null;

				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_PreencheNumerosSerieOrigemReserva", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : TrataReservasFechoLinha
		// Description : Efetua o lançamento relacionado com o fecho de linhas
		// Arguments   : IdLinhas -->
		// Arguments   : IdDoc    -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void TrataReservasFechoLinha(string IdLinhas)
		{
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			StdBE100.StdBELista objListaParciais = null;
			dynamic objOrigens = null;
			double dblQuantidade = 0;
			double dblQuantidadeRes = 0;
			dynamic objOrigensFinal = null;
			OrderedDictionary colIdDocsDestinos = null;


			try
			{

				//Atualiza as reservas
				if (Strings.Len(IdLinhas) > 0)
				{

					colIdDocsDestinos = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
					//Obter o identificador dos documentos destino da linha fechada
					strSQL = "SELECT DISTINCT C.Id FROM CabecDoc (NOLOCK) C" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN LinhasDoc (NOLOCK) L ON L.IdCabecDoc = C.Id" + Environment.NewLine;
					strSQL = strSQL + "LEFT JOIN LinhasDoc (NOLOCK) LEST ON LEST.IDLinhaEstorno = L.id" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN LinhasDocTrans (NOLOCK) LDT ON LDT.IdLinhasDoc = L.Id" + Environment.NewLine;
					strSQL = strSQL + "WHERE LEST.ID IS NULL AND LDT.IdLinhasDocOrigem IN (" + IdLinhas + ")";
					objLista = m_objErpBSO.Consulta(strSQL);
					dynamic tempRefParam = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam))
					{
						objLista = (StdBE100.StdBELista) tempRefParam;

						while (!objLista.NoFim())
						{

							colIdDocsDestinos.Add("" + objLista.Valor("Id"), "" + objLista.Valor("Id"));
							objLista.Seguinte();

						}

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam;
					}

					objLista = null;

					strSQL = "SELECT L.Id IdLinha, R.Fechada, R.Id, (LCS.Quantidade * L.FactorConv) - (LCS.QuantTrans * L.FactorConv) QtdReducao, R.Quantidade QtdRes, R.QuantidadePendente, LDT.NTrf" + Environment.NewLine;
					strSQL = strSQL + "FROM LinhasDoc (NOLOCK) L" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN INV_Reservas (NOLOCK) R ON R.IdChaveDestino = L.Id" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN (" + Environment.NewLine;
					strSQL = strSQL + " SELECT IdReserva, MAX(Data) Data FROM INV_Movimentos (NOLOCK)" + Environment.NewLine;
					strSQL = strSQL + " GROUP BY IdReserva" + Environment.NewLine;
					strSQL = strSQL + ") M ON M.IdReserva = R.Id" + Environment.NewLine;
					strSQL = strSQL + "LEFT JOIN (" + Environment.NewLine;
					strSQL = strSQL + "SELECT LDT.IdLinhasDocOrigem, COUNT(1) AS NTrf FROM LinhasDocTrans (NOLOCK) LDT" + Environment.NewLine;
					strSQL = strSQL + "LEFT JOIN LinhasDoc (NOLOCK) LEST ON LEST.IDLinhaEstorno = LDT.IdLinhasDoc" + Environment.NewLine;
					strSQL = strSQL + "WHERE LEST.Id Is Null" + Environment.NewLine;
					strSQL = strSQL + "GROUP BY IdLinhasDocOrigem" + Environment.NewLine;
					strSQL = strSQL + ") LDT ON LDT.IdLinhasDocOrigem = L.Id" + Environment.NewLine;
					strSQL = strSQL + "INNER JOIN LinhasDocStatus (NOLOCK) LCS ON LCS.IdLinhasDoc = L.Id" + Environment.NewLine;
					strSQL = strSQL + "WHERE L.Id IN (" + IdLinhas + ") AND LCS.QuantTrans <> LCS.Quantidade" + Environment.NewLine;
					strSQL = strSQL + "ORDER BY M.Data DESC" + Environment.NewLine;

					objLista = m_objErpBSO.Consulta(strSQL);
					dynamic tempRefParam2 = objLista;
					if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam2))
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;

						while (!objLista.NoFim())
						{

							dblQuantidadeRes = m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("QtdRes"));
							//Vamos verificar se existiu um desdobramento de linhas, se existiu, as quantidades têm que ser obtidas de outra forma
							if (m_objErpBSO.DSO.Plat.Utils.FDbl(StringsHelper.ToDoubleSafe(objLista.Valor("NTrf")) > 1) != 0)
							{

								strSQL = "SELECT L.Quantidade * L.FactorConv Qtd FROM LinhasDoc (NOLOCK) L" + Environment.NewLine;
								strSQL = strSQL + "INNER JOIN LinhasDocTrans (NOLOCK) LT ON LT.IdLinhasDoc = L.Id" + Environment.NewLine;
								strSQL = strSQL + "INNER JOIN INV_Reservas (NOLOCK) R ON R.IdChaveDestino = LT.IdLinhasDocOrigem AND R.Armazem = L.Armazem AND R.Localizacao = L.Localizacao AND " + Environment.NewLine;
								strSQL = strSQL + "                                      CASE WHEN ISNULL(R.Lote,'') = '<L01>' THEN '' ELSE ISNULL(R.Lote,'') END = CASE WHEN ISNULL(L.Lote,'') = '<L01>' THEN '' ELSE ISNULL(L.Lote,'') END" + Environment.NewLine;
								strSQL = strSQL + "WHERE LT.IdLinhasDocOrigem = '@1@' AND R.Id = '@2@'";
								dynamic[] tempRefParam3 = new dynamic[]{"" + objLista.Valor("IdLinha"), "" + objLista.Valor("Id")};
								strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam3);
								objListaParciais = m_objErpBSO.Consulta(strSQL);
								dynamic tempRefParam4 = objListaParciais;
								if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam4))
								{
									objListaParciais = (StdBE100.StdBELista) tempRefParam4;

									dblQuantidade = dblQuantidadeRes - m_objErpBSO.DSO.Plat.Utils.FDbl(objListaParciais.Valor("Qtd"));

								}
								else
								{
									objListaParciais = (StdBE100.StdBELista) tempRefParam4;

									dblQuantidade = m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("QtdReducao"));

								}

								objListaParciais = null;

							}
							else
							{

								dblQuantidade = m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("QtdReducao"));

							}

							//Se a quantidade da reserva é igual á quantidade da linha, então apagamos a reserva e os seus movimentos
							if (dblQuantidade >= dblQuantidadeRes)
							{

								m_objErpBSO.Inventario.Reservas.RemoveID("" + objLista.Valor("Id"));

							}
							else
							{

								//Actualiza a reserva
								strSQL = "UPDATE INV_Reservas SET Quantidade = Quantidade - @1@ WHERE Id = '@2@'";
								dynamic[] tempRefParam5 = new dynamic[]{dblQuantidade, "" + objLista.Valor("Id")};
								strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam5);
								DbCommand TempCommand = null;
								TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
								UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
								TempCommand.CommandText = strSQL;
								UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
								TempCommand.ExecuteNonQuery();

								if (StringsHelper.ToDoubleSafe(objLista.Valor("Fechada")) == 0)
								{

									//objOrigens = new dynamic();
									//objOrigensFinal = new dynamic();
									m_objErpBSO.Inventario.Reservas.PreencheOrigensReducaoReserva(objOrigens, "" + objLista.Valor("Id"), "" + objLista.Valor("Id"), dblQuantidade);
									foreach (dynamic objOrigem in objOrigens)
									{

										if (!FuncoesComuns100.FuncoesDS.FuncoesUtilsDS.VerificaExisteCollection(objOrigem.IdChave1, colIdDocsDestinos))
										{

											objOrigensFinal.Insere(objOrigem);

										}

									}

									VerificaNumerosSerieMovimentadosFechoLinha(IdLinhas, objOrigensFinal);

									m_objErpBSO.Inventario.Documentos.Actualiza(objOrigensFinal);
									objOrigens = null;
									objOrigensFinal = null;


								}

								//Actualiza a reserva
								strSQL = "UPDATE INV_Reservas SET QuantidadePendente = @1@ WHERE Id = '@2@'";
								dynamic[] tempRefParam6 = new dynamic[]{m_objErpBSO.DSO.Plat.Utils.FDbl(objLista.Valor("QuantidadePendente")) - dblQuantidade, "" + objLista.Valor("Id")};
								strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam6);
								DbCommand TempCommand_2 = null;
								TempCommand_2 = m_objErpBSO.DSO.BDAPL.CreateCommand();
								UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_2);
								TempCommand_2.CommandText = strSQL;
								UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_2);
								TempCommand_2.ExecuteNonQuery();

								strSQL = "UPDATE INV_Reservas SET Fechada = 1, QuantidadePendente = 0 WHERE Id = '@1@' AND QuantidadePendente <= 0";
								dynamic[] tempRefParam7 = new dynamic[]{"" + objLista.Valor("Id")};
								strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam7);
								DbCommand TempCommand_3 = null;
								TempCommand_3 = m_objErpBSO.DSO.BDAPL.CreateCommand();
								UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_3);
								TempCommand_3.CommandText = strSQL;
								UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_3);
								TempCommand_3.ExecuteNonQuery();

							}

							objLista.Seguinte();

						}

					}
					else
					{
						objLista = (StdBE100.StdBELista) tempRefParam2;
					}

				}

				objLista = null;
				objOrigens = null;
				objOrigensFinal = null;
				colIdDocsDestinos = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_TrataReservasFechoLinha", excep.Message);
			}

		}



		//---------------------------------------------------------------------------------------
		// Procedure   : VerificaNumerosSerieMovimentados
		// Description : Valida se os números de série movimentados no documento destino são os mesmos que os devolvidos pela redução, caso contrário ajusta
		// Arguments   : IdLinhas -->
		// Arguments   : Origens  -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void VerificaNumerosSerieMovimentadosFechoLinha(string IdLinhas, dynamic Origens)
		{
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			OrderedDictionary colNumSerie = null;
			dynamic objNumSerie = null;

			try
			{

				if (Origens == null || Strings.Len(IdLinhas) == 0)
				{

					return;

				}

				strSQL = "SELECT DISTINCT nsm.IdNumeroSerie, nsm.NumeroSerie FROM LinhasDoc (NOLOCK) L" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN LinhasDocTrans (NOLOCK) LT ON LT.IdLinhasDoc = L.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN Artigo (NOLOCK) A ON A.Artigo = L.Artigo AND A.TratamentoSeries = 1" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN INV_Origens (NOLOCK) O ON O.IdChave2 = L.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN INV_Movimentos (NOLOCK) M ON M.IdOrigem = O.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN INV_NumerosSerieMovimento (NOLOCK) NSM ON NSM.IdMovimentoStock = m.Id" + Environment.NewLine;
				strSQL = strSQL + "WHERE LT.IdLinhasDocOrigem IN (" + IdLinhas + ")";

				objLista = m_objErpBSO.Consulta(strSQL);
				dynamic tempRefParam = objLista;
				if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam))
				{
					objLista = (StdBE100.StdBELista) tempRefParam;

					colNumSerie = new OrderedDictionary(System.StringComparer.OrdinalIgnoreCase);
					while (!objLista.NoFim())
					{

						colNumSerie.Add("" + objLista.Valor("IdNumeroSerie"), "" + objLista.Valor("IdNumeroSerie") + "|" + "" + objLista.Valor("NumeroSerie"));
						objLista.Seguinte();

					}

					int tempForVar = Origens.NumItens;
					for (int lngIndiceOrg = 1; lngIndiceOrg <= tempForVar; lngIndiceOrg++)
					{

						int tempForVar2 = Origens.GetEdita(lngIndiceOrg).MovimentosStock.NumItens;
						for (int lngIndiceMov = 1; lngIndiceMov <= tempForVar2; lngIndiceMov++)
						{

							int tempForVar3 = Origens.GetEdita(lngIndiceOrg).MovimentosStock.GetEdita(lngIndiceMov).NumerosSerie.NumItens;
							for (int lngIndiceNSerie = 1; lngIndiceNSerie <= tempForVar3; lngIndiceNSerie++)
							{

								objNumSerie = Origens.GetEdita(lngIndiceOrg).MovimentosStock.GetEdita(lngIndiceMov).NumerosSerie.GetEdita(lngIndiceNSerie);

								if (lngIndiceNSerie <= colNumSerie.Count)
								{

									objNumSerie.ID = Strings.Split((string) colNumSerie[lngIndiceNSerie - 1], "|", -1, CompareMethod.Text)[0];
									objNumSerie.NumeroSerie = Strings.Split((string) colNumSerie[lngIndiceNSerie - 1], "|", -1, CompareMethod.Text)[1];

								}


							}

						}

					}

				}
				else
				{
					objLista = (StdBE100.StdBELista) tempRefParam;
				}


				objLista = null;
				colNumSerie = null;
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_VerificaNumerosSerieMovimentadosFechoLinha", excep.Message);
			}

		}

		//---------------------------------------------------------------------------------------
		// Procedure   : AbreLinhasECLFechadasAnulacao
		// Description : Permite abrir linhas fechadas na anulação de documentos em que a origem é um documento do tipo encomenda
		// Arguments   : IdDocumento -->
		// Arguments   : IdLinha     -->
		// Returns     : None
		//---------------------------------------------------------------------------------------
		private void AbreLinhasECLFechadasAnulacao(ref string IdDocumento, ref string IdLinha)
		{
			string strSQL = "";
			StdBE100.StdBELista objLista = null;
			string strIdsCabec = "";
			string strIdsLinhas = "";

			try
			{

				if (Strings.Len(IdDocumento) == 0 && Strings.Len(IdLinha) == 0)
				{

					return;

				}

				strSQL = "SELECT CORG.Id IdCabec, LORG.Id IdLinha FROM CabecDoc (NOLOCK) C" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN LinhasDoc (NOLOCK) L ON L.IdCabecDoc = C.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN LinhasDocTrans (NOLOCK) LDT ON LDT.IdLinhasDoc = L.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN LinhasDoc (NOLOCK) LORG ON LORG.Id = LDT.IdLinhasDocOrigem" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN CabecDoc (NOLOCK) CORG ON CORG.Id = LORG.IdCabecDoc" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN LinhasDocStatus (NOLOCK) LDS ON LDS.IdLinhasDoc = LORG.Id" + Environment.NewLine;
				strSQL = strSQL + "INNER JOIN DocumentosVenda (NOLOCK) D ON D.Documento = CORG.TipoDoc AND D.TipoDocumento = 2" + Environment.NewLine;
				strSQL = strSQL + "WHERE @1@ = '@2@' AND LDS.Fechado = 1" + Environment.NewLine;


				if (Strings.Len(IdDocumento) > 0)
				{

					dynamic[] tempRefParam = new dynamic[]{"C.Id", IdDocumento};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam);

				}
				else
				{

					dynamic[] tempRefParam2 = new dynamic[]{"L.Id", IdLinha};
					strSQL = m_objErpBSO.DSO.Plat.Sql.FormatSQL(strSQL, tempRefParam2);

				}

				objLista = m_objErpBSO.Consulta(strSQL);

				dynamic tempRefParam3 = objLista;
				if (!m_objErpBSO.DSO.Plat.FuncoesGlobais.IsNothingOrEmpty(tempRefParam3))
				{
					objLista = (StdBE100.StdBELista) tempRefParam3;

					while (!objLista.NoFim())
					{

						strIdsCabec = strIdsCabec + "'" + objLista.Valor("IdCabec") + "',";
						strIdsLinhas = strIdsLinhas + "'" + objLista.Valor("IdLinha") + "',";
						objLista.Seguinte();

					}

				}
				else
				{
					objLista = (StdBE100.StdBELista) tempRefParam3;
				}

				objLista = null;

				if (Strings.Len(strIdsCabec) > 0)
				{

					strSQL = "UPDATE CabecDocStatus SET Fechado = 0, Estado = 'P' WHERE IdCabecDoc IN (" + strIdsCabec.Substring(0, Math.Min(Strings.Len(strIdsCabec) - 1, strIdsCabec.Length)) + ")";
					DbCommand TempCommand = null;
					TempCommand = m_objErpBSO.DSO.BDAPL.CreateCommand();
					UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand);
					TempCommand.CommandText = strSQL;
					UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand);
					TempCommand.ExecuteNonQuery();

					strSQL = "UPDATE LinhasDocStatus SET Fechado = 0 WHERE IdLinhasDoc IN (" + strIdsLinhas.Substring(0, Math.Min(Strings.Len(strIdsLinhas) - 1, strIdsLinhas.Length)) + ")";
					DbCommand TempCommand_2 = null;
					TempCommand_2 = m_objErpBSO.DSO.BDAPL.CreateCommand();
					UpgradeHelpers.DB.DbConnectionHelper.ResetCommandTimeOut(TempCommand_2);
					TempCommand_2.CommandText = strSQL;
					UpgradeHelpers.DB.TransactionManager.SetCommandTransaction(TempCommand_2);
					TempCommand_2.ExecuteNonQuery();


				}
			}
			catch (System.Exception excep)
			{

				//UPGRADE_WARNING: (2081) Err.Number has a new behavior. More Information: http://www.vbtonet.com/ewis/ewi2081.aspx
				StdErros.StdRaiseErro(Information.Err().Number, "_AbreLinhasECLFechadasAnulacao", excep.Message);
			}

		}
		internal VndBSVendas()
		{
		}
	}
}