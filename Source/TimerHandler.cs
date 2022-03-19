using System;
using System.Threading;

namespace LEDSystem.Core.Handlers
{
    public class TimerHandler
    {
        #region Variables
        private Thread thread;
        private bool isAlive;
        private int interval;
        private Action closeAction;
        #endregion

        #region Controls
        /// <summary>
        /// Crea il timer.
        /// </summary>
        public void Create(Action createAction, Action updateAction, Action closeAction)
        {
            this.closeAction = closeAction;
            this.isAlive = false;

            thread = new Thread(() => {
                createAction();
                while (isAlive) {
                    updateAction();
                    Thread.Sleep(interval);
                }
            });

            // Avvia il timer.
            this.Start();
        }
        /// <summary>
        /// Avvia il timer.
        /// </summary>
        public void Start()
        {
            // Annulla la funzione se il thread non esiste.
            if (thread == null) return;

            // Avvia il thread.
            isAlive = true;
            thread.Start();
        }
        /// <summary>
        /// Arresta il timer
        /// </summary>
        public void Stop()
        {
            // Annulla la funzione se il thread non esiste.
            if (thread == null) return;

            // Chiama la funzione di chiusura dell'effetto in esecuzione.
            closeAction?.Invoke();

            // Arresta il thread.
            if (thread.IsAlive && isAlive) {
                isAlive = false;
                thread = null;
            }
        }
        /// <summary>
        /// Imposta un nuovo intervallo al timer.
        /// </summary>
        public void SetInterval(int interval)
        {
            this.interval = interval;
        }
        #endregion
    }
}
