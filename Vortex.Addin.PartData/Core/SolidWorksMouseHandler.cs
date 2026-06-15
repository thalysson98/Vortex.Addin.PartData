using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;

public class SolidWorksMouseHandler:Mouse
{
    private SldWorks swApp;
    private ModelDoc2 swModel;
    private ModelView swModelView;
    private string stComponente;
    private Mouse swMouse;

    public void Init(object mouse, string componente, ModelView view, ModelDoc2 model, SldWorks app)
    {
        swApp = app;
        swModel = model;
        swModelView = view;
        swMouse = (Mouse)mouse;
        stComponente = componente;
        swMouse.MouseLBtnDownNotify += MouseLBtnDownNotify;
        
    }

    public int MouseLBtnDownNotify(int x, int y, int wParam)
    {
        if (swModelView != null)
        {
            MathTransform ModelViewTransform;
            MathUtility swMathUtil;
            MathPoint swPt;
            AssemblyDoc swAssy;
            double[] nPt = new double[3];

            ModelViewTransform = swModelView.Transform;
            swMathUtil = (MathUtility)swApp.GetMathUtility();
            

            nPt[0] = x;
            nPt[1] = y;
            nPt[2] = 0;

            swPt = (MathPoint)swMathUtil.CreatePoint(nPt);
            swPt = (MathPoint)swPt.MultiplyTransform(ModelViewTransform.IInverse());

            double[] pointData = (double[])swPt.ArrayData;

            swAssy = (AssemblyDoc)swModel;

            // Insere sempre na configuração "simplificado" (independente de maiúsc/minúsc).
            // Se a peça não tiver essa configuração, configName fica "" e o SolidWorks
            // usa a última configuração salva.
            string configName = ResolverConfiguracao(stComponente);

            swAssy.AddComponent5(stComponente,
                (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig,
                "", false, configName,
                pointData[0], pointData[1], pointData[2]);

            swModelView = null;
            swMouse.MouseLBtnDownNotify -= MouseLBtnDownNotify;
        }
        return 0;
    }

    // Procura uma configuração chamada "simplificado" (qualquer combinação de
    // maiúsculas/minúsculas) lendo o arquivo sem precisar abri-lo.
    // Retorna o nome com a grafia real da configuração, ou "" se não existir.
    private string ResolverConfiguracao(string caminho)
    {
        try
        {
            object names = swApp.GetConfigurationNames(caminho);
            if (names is string[] configs)
            {
                foreach (string nome in configs)
                    if (string.Equals(nome, "simplificado", StringComparison.OrdinalIgnoreCase))
                        return nome;
            }
        }
        catch { }
        return ""; // não encontrada → última configuração salva
    }

    public bool Move(int X, int Y, int Flags)
    {
        return swMouse.Move(X, Y, Flags);
    }

    public bool MoveXYZ(double X, double Y, double Z, int Flags)
    {
        return swMouse.MoveXYZ(X, Y, Z, Flags);
    }

    public bool MouseWheelXYZ(double X, double Y, double Z, int Clicks, int Flags)
    {
        return swMouse.MouseWheelXYZ(X, Y, Z, Clicks, Flags);
    }

    public event DMouseEvents_MouseNotifyEventHandler MouseNotify
    {
        add
        {
            swMouse.MouseNotify += value;
        }

        remove
        {
            swMouse.MouseNotify -= value;
        }
    }

    public event DMouseEvents_MouseMoveNotifyEventHandler MouseMoveNotify
    {
        add
        {
            swMouse.MouseMoveNotify += value;
        }

        remove
        {
            swMouse.MouseMoveNotify -= value;
        }
    }

    event DMouseEvents_MouseLBtnDownNotifyEventHandler DMouseEvents_Event.MouseLBtnDownNotify
    {
        add
        {
            swMouse.MouseLBtnDownNotify += value;
        }

        remove
        {
            swMouse.MouseLBtnDownNotify -= value;
        }
    }

    public event DMouseEvents_MouseLBtnUpNotifyEventHandler MouseLBtnUpNotify
    {
        add
        {
            swMouse.MouseLBtnUpNotify += value;
        }

        remove
        {
            swMouse.MouseLBtnUpNotify -= value;
        }
    }

    public event DMouseEvents_MouseRBtnDownNotifyEventHandler MouseRBtnDownNotify
    {
        add
        {
            swMouse.MouseRBtnDownNotify += value;
        }

        remove
        {
            swMouse.MouseRBtnDownNotify -= value;
        }
    }

    public event DMouseEvents_MouseRBtnUpNotifyEventHandler MouseRBtnUpNotify
    {
        add
        {
            swMouse.MouseRBtnUpNotify += value;
        }

        remove
        {
            swMouse.MouseRBtnUpNotify -= value;
        }
    }

    public event DMouseEvents_MouseMBtnDownNotifyEventHandler MouseMBtnDownNotify
    {
        add
        {
            swMouse.MouseMBtnDownNotify += value;
        }

        remove
        {
            swMouse.MouseMBtnDownNotify -= value;
        }
    }

    public event DMouseEvents_MouseMBtnUpNotifyEventHandler MouseMBtnUpNotify
    {
        add
        {
            swMouse.MouseMBtnUpNotify += value;
        }

        remove
        {
            swMouse.MouseMBtnUpNotify -= value;
        }
    }

    public event DMouseEvents_MouseLBtnDblClkNotifyEventHandler MouseLBtnDblClkNotify
    {
        add
        {
            swMouse.MouseLBtnDblClkNotify += value;
        }

        remove
        {
            swMouse.MouseLBtnDblClkNotify -= value;
        }
    }

    public event DMouseEvents_MouseRBtnDblClkNotifyEventHandler MouseRBtnDblClkNotify
    {
        add
        {
            swMouse.MouseRBtnDblClkNotify += value;
        }

        remove
        {
            swMouse.MouseRBtnDblClkNotify -= value;
        }
    }

    public event DMouseEvents_MouseMBtnDblClkNotifyEventHandler MouseMBtnDblClkNotify
    {
        add
        {
            swMouse.MouseMBtnDblClkNotify += value;
        }

        remove
        {
            swMouse.MouseMBtnDblClkNotify -= value;
        }
    }

    public event DMouseEvents_MouseSelectNotifyEventHandler MouseSelectNotify
    {
        add
        {
            swMouse.MouseSelectNotify += value;
        }

        remove
        {
            swMouse.MouseSelectNotify -= value;
        }
    }
}
