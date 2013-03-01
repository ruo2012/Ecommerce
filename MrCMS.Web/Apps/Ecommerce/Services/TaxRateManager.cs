﻿using System.Collections.Generic;
using MrCMS.Web.Apps.Ecommerce.Entities;
using NHibernate;
using MrCMS.Helpers;
using System.Linq;

namespace MrCMS.Web.Apps.Ecommerce.Services
{
    public class TaxRateManager : ITaxRateManager
    {
        private readonly ISession _session;

        public TaxRateManager(ISession session)
        {
            _session = session;
        }

        public IList<TaxRate> GetAll()
        {
            return _session.QueryOver<TaxRate>().Cacheable().List();
        }

        public void Add(TaxRate taxRate)
        {
            _session.Transact(session => session.Save(taxRate));
        }

        public void Update(TaxRate taxRate)
        {
            _session.Transact(session => session.Update(taxRate));
        }

        public void Delete(TaxRate taxRate)
        {
            _session.Transact(session => session.Delete(taxRate));
        }
    }
}